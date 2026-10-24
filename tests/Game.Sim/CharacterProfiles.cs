using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Sim;

/// <summary>Onde vão os pontos de atributo do perfil (T-206).</summary>
public enum AttributePlan
{
    /// <summary>Todos nos atributos sugeridos da classe (§3), divididos por igual (o primeiro leva a sobra).</summary>
    Suggested,

    /// <summary>Metade nos atributos sugeridos e a outra metade dividida entre os demais (decisão do designer, balance-v0.7 §4).</summary>
    Spread,
}

/// <summary>
/// Perfil de personagem do simulador (T-206, <c>design/progression-v0.1.md</c> §8): nível, atributos, equipamento completo
/// do nível (todos os espaços, raridade escolhida) e distribuição dos pontos de habilidade que o nível permite.
/// O "perfil esperado" de uma área é o nível dela com atributos sugeridos e equipamento comum; o "fraco", 3 níveis abaixo.
/// </summary>
public sealed record CharacterProfile
{
    public int Level { get; init; } = 1;
    public AttributePlan Attributes { get; init; } = AttributePlan.Suggested;
    public ItemRarity Gear { get; init; } = ItemRarity.Common;

    /// <summary>Nível dos itens; null = <see cref="Level"/>.</summary>
    public int? GearLevel { get; init; }

    /// <summary>Distribuição dos pontos de habilidade: <see cref="RankProfiles.Focused"/> ou <see cref="RankProfiles.Spread"/>.</summary>
    public string Skills { get; init; } = RankProfiles.Focused;

    /// <summary>Par do perfil focado por classe, por cima de <see cref="RankProfiles.FocusedPair"/> (ex.: Clérigo solo com Corrente e Escudo).</summary>
    public IReadOnlyDictionary<string, string[]> Focus { get; init; } = new Dictionary<string, string[]>();

    /// <summary>NH explícitos por classe (precisam valer para o nível); substituem <see cref="Skills"/> para essa classe.</summary>
    public IReadOnlyDictionary<string, Dictionary<string, int>> Ranks { get; init; } = new Dictionary<string, Dictionary<string, int>>();

    public string Label =>
        $"nv{Level} {(Attributes == AttributePlan.Suggested ? "sugerido" : "espalhado")} {CharacterProfiles.RarityName(Gear)}"
        + (GearLevel is { } g && g != Level ? $"{g}" : "") + $" NH:{(Ranks.Count > 0 ? "explícito" : Skills)}";

    public static CharacterProfile Expected(int level) => new() { Level = level };

    /// <summary>3 níveis abaixo (mínimo 1), com equipamento comum do próprio nível.</summary>
    public static CharacterProfile Weak(int level) => new() { Level = Math.Max(1, level - 3) };
}

public static class CharacterProfiles
{
    /// <summary>Atributos sugeridos por classe (§3, tabela do perfil inicial).</summary>
    public static readonly IReadOnlyDictionary<string, AttributeKind[]> Suggested = new Dictionary<string, AttributeKind[]>
    {
        ["class.warrior"] = [AttributeKind.Str, AttributeKind.Vit],
        ["class.archer"] = [AttributeKind.Dex, AttributeKind.Agi],
        ["class.mage"] = [AttributeKind.Int],
        ["class.cleric"] = [AttributeKind.Int, AttributeKind.Vit],
    };

    /// <summary>Afixos escolhidos para incomuns (o primeiro) e raros (os dois), pelo atributo sugerido principal da classe.</summary>
    private static string[] AffixesFor(string classId) =>
        [$"affix.{Suggested[classId][0].ToString().ToLowerInvariant()}", "affix.health"];

    /// <summary>Cria o personagem do perfil (com progressão), equipado e com vida e Mana cheias.</summary>
    public static Entity Spawn(World world, ClassDefinition cls, System.Numerics.Vector2 position, CharacterProfile profile)
    {
        var content = world.Content;
        var e = Leveling.SpawnCharacter(world, cls, position, BuildFor(content, cls, profile));
        var eq = e.Get<Equipment>();
        foreach (var (slot, item) in GearFor(content, cls, profile))
        {
            eq[slot] = item;
        }

        Gear.Refresh(world, e);
        var health = e.Get<Health>();
        health.Current = health.Max;
        if (e.TryGet<ResourcePool>() is { } pool)
        {
            pool.Current = pool.Max;
        }

        return e;
    }

    public static CharacterBuild BuildFor(ContentCatalog content, ClassDefinition cls, CharacterProfile profile)
    {
        var rules = content.Rules.Progression;
        var level = Math.Clamp(profile.Level, 1, rules.MaxLevel);
        var points = rules.AttributePointsAt(level);
        var suggested = Suggested.TryGetValue(cls.Id, out var s) ? s : Enum.GetValues<AttributeKind>();
        var given = new int[5];
        if (profile.Attributes == AttributePlan.Suggested)
        {
            Split(points, suggested, given);
        }
        else
        {
            var half = points / 2;
            Split(points - half, suggested, given);
            Split(half, Enum.GetValues<AttributeKind>().Except(suggested).ToArray(), given);
        }

        return new CharacterBuild
        {
            Level = level,
            Allocated = new AttributeSet { Str = given[0], Dex = given[1], Agi = given[2], Int = given[3], Vit = given[4] },
            SkillRanks = profile.Ranks.TryGetValue(cls.Id, out var explicitRanks)
                ? explicitRanks
                : SkillRanks(content, cls, level, profile.Skills, profile.Focus.TryGetValue(cls.Id, out var focus) ? focus : null),
        };
    }

    /// <summary>Divide os pontos por igual entre os atributos (os primeiros levam a sobra).</summary>
    private static void Split(int points, AttributeKind[] attrs, int[] into)
    {
        for (var i = 0; i < attrs.Length; i++)
        {
            into[(int)attrs[i]] += (points / attrs.Length) + (i < points % attrs.Length ? 1 : 0);
        }
    }

    /// <summary>
    /// NH que o nível permite (1 ponto por nível; NH n exige nível 2n − 1, assinatura 12/13/15). Os pontos vão um a um:
    /// primeiro cada normal no NH1; depois, no focado, as duas habilidades do par (<see cref="RankProfiles.FocusedPair"/>)
    /// alternando, a assinatura e o resto; no espalhado, sempre a de menor NH (normais antes da assinatura no empate).
    /// No nível 15 dá os mesmos perfis da T-063 (5+5+1+1+3 e 3+3+3+3+3).
    /// </summary>
    public static Dictionary<string, int> SkillRanks(ContentCatalog content, ClassDefinition cls, int level, string plan, string[]? focus = null)
    {
        if (plan != RankProfiles.Focused && plan != RankProfiles.Spread)
        {
            throw new ArgumentException($"Distribuição de NH desconhecida '{plan}'. Use {RankProfiles.Focused} ou {RankProfiles.Spread}.");
        }

        var rules = content.Rules.Progression;
        var normals = cls.Abilities.Where(id => Leveling.IsLearnable(cls, id) && !content.Abilities[id].Signature).ToList();
        var signature = cls.Abilities.FirstOrDefault(id => content.Abilities[id].Signature);
        var pair = focus ?? (RankProfiles.FocusedPair.TryGetValue(cls.Id, out var p) ? p : normals.Take(2).ToArray());
        var ranks = cls.Abilities.Where(id => Leveling.IsLearnable(cls, id)).ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        bool CanRaise(string id) =>
            ranks[id] < content.Abilities[id].HighestRank && level >= rules.RequiredLevel(content.Abilities[id].Signature, ranks[id] + 1);

        string? Next()
        {
            if (normals.OrderBy(id => pair.Contains(id) ? 0 : 1).FirstOrDefault(id => ranks[id] == 0) is { } unlearned)
            {
                return unlearned;
            }

            IEnumerable<string> order = plan == RankProfiles.Focused
                ? pair.OrderBy(id => ranks[id]).Concat(signature is null ? [] : [signature]).Concat(normals.Except(pair).OrderBy(id => ranks[id]))
                : normals.Concat(signature is null ? [] : [signature]).OrderBy(id => ranks[id]);
            return order.FirstOrDefault(CanRaise);
        }

        for (var points = rules.SkillPointsAt(level); points > 0 && Next() is { } id; points--)
        {
            ranks[id]++;
        }

        return ranks.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Equipamento completo do perfil: a arma do tipo inicial da classe, a armadura mais pesada que ela usa em cada espaço
    /// e um anel, todos no nível do perfil; incomuns com o afixo do atributo principal, raros com ele e vida.
    /// </summary>
    public static IEnumerable<(EquipSlot Slot, ItemInstance Item)> GearFor(ContentCatalog content, ClassDefinition cls, CharacterProfile profile)
    {
        var rules = content.Rules.Items;
        var level = profile.GearLevel ?? profile.Level;
        var affixes = AffixesFor(cls.Id).Take((int)rules.AffixCount.For(profile.Gear)).ToArray();
        ItemInstance Make(string baseId) => new(baseId, level, profile.Gear, affixes);

        var weapon = cls.StartingEquipment.Select(s => content.Items[s.Item]).FirstOrDefault(i => i.Slot == EquipSlot.Weapon);
        if (weapon is not null)
        {
            yield return (EquipSlot.Weapon, Make(weapon.Id));
        }

        foreach (var slot in new[] { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Feet })
        {
            var armor = content.Items.Values
                .Where(i => i.Slot == slot && i.ArmorType is not null && ItemMath.CanUse(rules, i, cls.Id))
                .OrderByDescending(i => rules.ArmorTypes.For(i.ArmorType!.Value).ArmorMultiplier)
                .ThenBy(i => i.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (armor is not null)
            {
                yield return (slot, Make(armor.Id));
            }
        }

        if (content.Items.Values.Where(i => i.Slot == EquipSlot.Accessory && ItemMath.CanUse(rules, i, cls.Id))
                .OrderBy(i => i.Id, StringComparer.Ordinal).LastOrDefault() is { } accessory)
        {
            yield return (EquipSlot.Accessory, Make(accessory.Id));
        }
    }

    public static string RarityName(ItemRarity r) => r switch
    {
        ItemRarity.Common => "comum",
        ItemRarity.Uncommon => "incomum",
        ItemRarity.Rare => "raro",
        _ => "épico",
    };
}
