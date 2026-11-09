using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Server.Persistence;

/// <summary>
/// O que se grava de um personagem (T-212): build (nível, XP, pontos, NH), equipamento e inventário (T-320: com pilhas) e
/// ouro. Imutável: é capturado na thread do loop e gravado em outra. <see cref="Equipment"/> null = personagem novo, com o
/// equipamento e o inventário iniciais da classe.
/// </summary>
public sealed record CharacterState(CharacterBuild Build, IReadOnlyDictionary<EquipSlot, ItemInstance>? Equipment, IReadOnlyList<ItemInstance?> Inventory)
{
    public static CharacterState New { get; } = new(new CharacterBuild(), null, []);

    /// <summary>Ouro na carteira (T-320).</summary>
    public long Gold { get; init; }

    /// <summary>
    /// T-320: veio de um documento v1 (antes dos consumíveis como item). Ao criar a entidade, se o inventário não tem nenhum
    /// item do inventário inicial da classe, ele recebe esse inventário (as 5 poções de vida).
    /// </summary>
    public bool GrantStartingInventory { get; init; }

    /// <summary>Zona e posição (T-214, colunas <c>zone_id</c>, <c>position_x/y</c>); null = não mudam ao gravar (sala de encontro).</summary>
    public CharacterLocation? Location { get; init; }

    /// <summary>
    /// Recargas levadas na troca de zona (T-301; T-320: inclusive as dos consumíveis). Não vai para o banco: sair do jogo e
    /// voltar zera as recargas. Null = tudo pronto.
    /// </summary>
    public AbilityTimers? Timers { get; init; }

    /// <summary>Lê o estado de uma entidade criada por <see cref="Spawn"/> (null se não tem progressão).</summary>
    public static CharacterState? Capture(Entity e)
    {
        if (Leveling.BuildOf(e) is not { } build)
        {
            return null;
        }

        var equipment = e.TryGet<Equipment>()?.Items.ToDictionary(x => x.Slot, x => x.Item) ?? [];
        var inventory = e.TryGet<Inventory>()?.Slots.ToArray() ?? [];
        return new CharacterState(build, equipment, inventory) { Gold = e.TryGet<Wallet>()?.Gold ?? 0 };
    }

    /// <summary>
    /// Cria a entidade com o estado salvo: build validado (lança <see cref="ArgumentException"/> se não vale), equipamento e
    /// inventário. Itens que o catálogo não conhece mais, ou fora do espaço certo, são descartados e listados em
    /// <paramref name="dropped"/>; pilhas acima do máximo do item são cortadas. Vida e Mana começam cheias. Personagem novo
    /// (<see cref="Equipment"/> null) fica com o inventário inicial da classe (T-320).
    /// </summary>
    public Entity Spawn(World world, ClassDefinition cls, Vector2 position, List<string>? dropped = null)
    {
        var e = Leveling.SpawnCharacter(world, cls, position, Build);
        if (Equipment is not null)
        {
            var equipment = new Equipment();
            foreach (var (slot, item) in Equipment)
            {
                if (world.Content.Items.TryGetValue(item.BaseId, out var def) && def.Slot == slot)
                {
                    equipment[slot] = item;
                }
                else
                {
                    dropped?.Add($"{slot}: {item.BaseId}");
                }
            }

            e.Add(equipment);

            // Personagem gravado: o inventário é o salvo, não o inicial que a classe deu ao criar a entidade.
            var start = e.Get<Inventory>();
            for (var i = 0; i < start.Capacity; i++)
            {
                start[i] = null;
            }
        }

        var inventory = e.Get<Inventory>();
        for (var i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i] is not { } item)
            {
                continue;
            }

            if (i < inventory.Capacity && world.Content.Items.TryGetValue(item.BaseId, out var def) && item.Count >= 1)
            {
                inventory[i] = item.Count > def.MaxStack ? item.WithCount(def.MaxStack) : item;
            }
            else
            {
                dropped?.Add($"inventário {i}: {item.BaseId}");
            }
        }

        if (GrantStartingInventory && Equipment is not null
            && !inventory.Slots.Any(i => i is not null && cls.StartingInventory.Any(s => s.Item == i.BaseId)))
        {
            EntityFactory.AddStartingInventory(world, cls, e);
        }

        if (e.TryGet<Wallet>() is { } wallet)
        {
            wallet.Gold = Math.Max(0, Gold);
        }

        ItemMath.Recompute(world.Content, cls, e.Get<Game.Core.Combat.Stats.CharacterStats>(), e.TryGet<CharacterProgress>(), e.Get<Equipment>());
        Leveling.RefreshDerived(world, e);
        var health = e.Get<Health>();
        health.Current = health.Max;
        if (e.TryGet<ResourcePool>() is { } pool && cls.Resource.Initial >= cls.Resource.Max)
        {
            pool.Current = pool.Max;
        }

        if (Timers is not null && e.TryGet<AbilityState>() is { } abilities)
        {
            Timers.ApplyTo(abilities, world.Tick, ItemAbilities(world));
        }

        return e;
    }

    /// <summary>Habilidades usadas por consumíveis (T-320): a recarga delas acompanha a troca de zona sem estarem nas conhecidas.</summary>
    private static HashSet<string> ItemAbilities(World world) =>
        world.Content.Items.Values.Where(i => i.UseAbility is not null).Select(i => i.UseAbility!).ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// Recargas de um personagem independentes do tick da zona (T-301): ticks <b>restantes</b> de cada recarga (cada zona tem o
/// seu tick, então o tick absoluto não serve) e cargas por recarga em uso (T-060). Não há recarga global separada para
/// descartar: tudo que está em <see cref="AbilityState.ReadyAtTick"/> é por habilidade (T-320: inclusive as de consumíveis).
/// </summary>
public sealed record AbilityTimers(
    IReadOnlyDictionary<string, long> CooldownRemaining,
    IReadOnlyDictionary<string, int> CooldownChargesSpent)
{
    public static AbilityTimers Capture(AbilityState st, long tick) => new(
        st.ReadyAtTick.Select(kv => (kv.Key, Remaining: kv.Value - tick)).Where(x => x.Remaining > 0)
            .ToDictionary(x => x.Key, x => x.Remaining, StringComparer.Ordinal),
        st.HasCooldownChargesSpent ? new Dictionary<string, int>(st.CooldownChargesSpent, StringComparer.Ordinal) : []);

    /// <summary>
    /// Recria o estado no tick da zona nova; habilidades que o personagem não conhece mais são ignoradas, menos as de
    /// consumíveis (<paramref name="itemAbilities"/>, T-320).
    /// </summary>
    public void ApplyTo(AbilityState st, long tick, IReadOnlySet<string>? itemAbilities = null)
    {
        foreach (var (id, remaining) in CooldownRemaining)
        {
            if (st.Knows(id) || itemAbilities?.Contains(id) == true)
            {
                st.ReadyAtTick[id] = tick + remaining;
            }
        }

        foreach (var (id, spent) in CooldownChargesSpent)
        {
            if (st.Knows(id) && spent > 0)
            {
                st.CooldownChargesSpent[id] = spent;
            }
        }
    }
}

/// <summary>Onde o personagem está: zona e posição em tiles. Posição (0, 0) = ponto de nascimento da zona.</summary>
public sealed record CharacterLocation(string ZoneId, float X, float Y);

/// <summary>
/// Formato do documento <c>characters.progression</c> (jsonb), versão <see cref="Version"/>. Nível e XP ficam nas colunas
/// <c>level</c> e <c>experience</c>. O documento vazio (<c>{}</c>, gravado pela API ao criar) é o personagem novo.
/// <code>
/// { "allocated": { "str": 0, "dex": 0, "agi": 0, "int": 0, "vit": 0 },
///   "skillRanks": { "warrior.heavy_strike": 1 },
///   "equipment": { "weapon": { "base": "weapon.sword", "level": 2, "rarity": "common", "affixes": [] } },
///   "inventory": [ { "slot": 3, "item": { ..., "count": 5 } } ],
///   "gold": 120 }
/// </code>
/// Versão 2 (T-320): <c>gold</c> e <c>count</c> nos itens (ausente = 1). A versão 1 continua sendo lida: ouro 0 e, se o
/// inventário não tem nenhum item do inventário inicial da classe, ele é dado na carga (<see cref="CharacterState.GrantStartingInventory"/>).
/// </summary>
public static class CharacterDocument
{
    public const int Version = 2;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public static string Write(CharacterState state)
    {
        var doc = new Doc
        {
            Allocated = state.Build.Allocated,
            SkillRanks = state.Build.SkillRanks.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(),
            Equipment = state.Equipment?.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => ItemDoc.Of(kv.Value)),
            Inventory = state.Inventory.Select((item, slot) => (item, slot)).Where(x => x.item is not null)
                .Select(x => new SlotDoc { Slot = x.slot, Item = ItemDoc.Of(x.item!) }).ToList(),
            Gold = state.Gold,
        };
        return JsonSerializer.Serialize(doc, Json);
    }

    /// <summary>Lê o documento; lança <see cref="JsonException"/> ou <see cref="InvalidDataException"/> se está corrompido.</summary>
    public static CharacterState Read(int version, string json, int level, long experience)
    {
        if (version is not (1 or Version))
        {
            throw new InvalidDataException($"versão de progressão {version} desconhecida (esperada 1 ou {Version})");
        }

        var doc = JsonSerializer.Deserialize<Doc>(json, Json) ?? throw new InvalidDataException("documento de progressão nulo");
        var build = new CharacterBuild
        {
            Level = level,
            Experience = checked((int)experience),
            Allocated = doc.Allocated ?? new AttributeSet(),
            SkillRanks = doc.SkillRanks ?? [],
        };
        if (doc.Equipment is null)
        {
            return CharacterState.New with { Build = build };
        }

        var inventory = new List<ItemInstance?>();
        foreach (var s in doc.Inventory ?? [])
        {
            if (s.Slot is < 0 or > 1000 || s.Item is null)
            {
                throw new InvalidDataException($"espaço de inventário inválido: {s.Slot}");
            }

            while (inventory.Count <= s.Slot)
            {
                inventory.Add(null);
            }

            inventory[s.Slot] = s.Item.ToItem();
        }

        if (doc.Gold is < 0)
        {
            throw new InvalidDataException($"ouro negativo: {doc.Gold}");
        }

        return new CharacterState(build, doc.Equipment.ToDictionary(kv => kv.Key, kv => kv.Value.ToItem()), inventory)
        {
            Gold = version == 1 ? 0 : doc.Gold ?? 0,
            GrantStartingInventory = version == 1,
        };
    }

    private sealed class Doc
    {
        public AttributeSet? Allocated { get; set; }
        public Dictionary<string, int>? SkillRanks { get; set; }
        public Dictionary<EquipSlot, ItemDoc>? Equipment { get; set; }
        public List<SlotDoc>? Inventory { get; set; }
        public long? Gold { get; set; }
    }

    private sealed class SlotDoc
    {
        public int Slot { get; set; }
        public ItemDoc? Item { get; set; }
    }

    private sealed class ItemDoc
    {
        public string Base { get; set; } = "";
        public int Level { get; set; }
        public ItemRarity Rarity { get; set; }
        public List<string> Affixes { get; set; } = [];

        /// <summary>Unidades da pilha (v2); ausente = 1.</summary>
        public int? Count { get; set; }

        public static ItemDoc Of(ItemInstance i) =>
            new() { Base = i.BaseId, Level = i.Level, Rarity = i.Rarity, Affixes = [.. i.Affixes], Count = i.Count == 1 ? null : i.Count };

        public ItemInstance ToItem() => Base.Length == 0
            ? throw new InvalidDataException("item sem base")
            : Count is < 1
                ? throw new InvalidDataException($"quantidade inválida: {Count}")
                : new ItemInstance(Base, Level, Rarity, Affixes, Count ?? 1);
    }
}
