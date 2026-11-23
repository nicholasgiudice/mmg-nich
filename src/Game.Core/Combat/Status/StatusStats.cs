using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Status;

/// <summary>Atributos e Mana por segundo que um status soma (T-421). Valores inteiros nos atributos, como no personagem.</summary>
public readonly record struct StatBlock(int Str, int Dex, int Agi, int Int, int Vit, float ManaRegen)
{
    public static readonly StatBlock Zero = default;

    public bool IsZero => this == Zero;

    public bool HasAttributes => Str != 0 || Dex != 0 || Agi != 0 || Int != 0 || Vit != 0;

    public static StatBlock operator +(StatBlock a, StatBlock b) =>
        new(a.Str + b.Str, a.Dex + b.Dex, a.Agi + b.Agi, a.Int + b.Int, a.Vit + b.Vit, a.ManaRegen + b.ManaRegen);

    public static StatBlock operator -(StatBlock a, StatBlock b) =>
        new(a.Str - b.Str, a.Dex - b.Dex, a.Agi - b.Agi, a.Int - b.Int, a.Vit - b.Vit, a.ManaRegen - b.ManaRegen);

    public static StatBlock operator *(StatBlock a, int k) =>
        new(a.Str * k, a.Dex * k, a.Agi * k, a.Int * k, a.Vit * k, a.ManaRegen * k);

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Str);
        hasher.Add(Dex);
        hasher.Add(Agi);
        hasher.Add(Int);
        hasher.Add(Vit);
        hasher.Add(ManaRegen);
    }
}

/// <summary>
/// Contabilidade dos atributos vindos de status (T-421), interna do servidor: o valor de cada instância (calculado ao aplicar,
/// pelas estatísticas de quem aplicou) e o total que já está somado em <see cref="CharacterStats"/>. Não vai pela rede: o
/// cliente vê os atributos finais no próprio <see cref="CharacterStats"/>.
/// </summary>
public sealed class StatusStatBonuses : IComponent
{
    /// <summary>Valor por instância de status (id da instância, por entidade), por acúmulo.</summary>
    public SortedDictionary<int, StatBlock> ByInstance { get; } = new();

    /// <summary>O que já está somado nas estatísticas do personagem.</summary>
    public StatBlock Applied { get; set; }

    public IComponent Clone()
    {
        var c = new StatusStatBonuses { Applied = Applied };
        foreach (var (id, block) in ByInstance)
        {
            c.ByInstance[id] = block;
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        Applied.WriteHash(hasher);
        hasher.Add(ByInstance.Count);
        foreach (var (id, block) in ByInstance)
        {
            hasher.Add(id);
            block.WriteHash(hasher);
        }
    }
}

/// <summary>
/// Atributos e regeneração de Mana vindos de status (T-421, <c>stats</c> do status). Quem aplica guarda o valor
/// (<see cref="Capture"/>); a cada mudança dos status ativos, <see cref="Sync"/> acerta a diferença em
/// <see cref="CharacterStats"/> e refaz vida e Mana máximas (<see cref="Leveling.RefreshDerived"/>), como o equipamento.
/// </summary>
public static class StatusStats
{
    /// <summary>Calcula e guarda o valor da instância pelas estatísticas de <paramref name="source"/> (null = só a base).</summary>
    public static void Capture(Entity? source, Entity target, StatusInstance inst, StatusStatsDefinition def)
    {
        if (!target.Has<CharacterStats>())
        {
            return; // monstros não têm atributos: o status não muda nada neles
        }

        var bonuses = target.TryGet<StatusStatBonuses>();
        if (bonuses is null)
        {
            bonuses = new StatusStatBonuses();
            target.Add(bonuses);
        }

        bonuses.ByInstance[inst.Id] = Evaluate(def, source);
    }

    /// <summary>O valor de <paramref name="def"/> para uma fonte (um acúmulo).</summary>
    public static StatBlock Evaluate(StatusStatsDefinition def, Entity? source)
    {
        int Attr(string key)
        {
            var total = 0.0;
            if (def.Attributes.TryGetValue(key, out var v))
            {
                total += StatMath.Scaled(v.Base, v.Scaling, source);
            }

            if (def.Attributes.TryGetValue("all", out var all))
            {
                total += StatMath.Scaled(all.Base, all.Scaling, source);
            }

            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        var mana = def.ManaRegen is { } m ? (float)StatMath.Scaled(m.Base, m.Scaling, source) : 0f;
        return new StatBlock(Attr("str"), Attr("dex"), Attr("agi"), Attr("int"), Attr("vit"), mana);
    }

    /// <summary>
    /// Acerta as estatísticas pelos status ativos agora: soma a diferença entre o que deveria estar somado e o que já está, e
    /// refaz os derivados se algum atributo mudou. Sem nada de status (nunca recebeu um com <c>stats</c>), não faz nada.
    /// </summary>
    public static void Sync(World world, Entity e)
    {
        if (e.TryGet<StatusStatBonuses>() is not { } bonuses || e.TryGet<CharacterStats>() is not { } s)
        {
            return;
        }

        var active = StatusManager.Active(world, e).ToDictionary(x => x.Id);
        var desired = StatBlock.Zero;
        foreach (var id in bonuses.ByInstance.Keys.ToList())
        {
            if (active.TryGetValue(id, out var inst))
            {
                desired += bonuses.ByInstance[id] * inst.Stacks;
            }
            else
            {
                bonuses.ByInstance.Remove(id);
            }
        }

        var delta = desired - bonuses.Applied;
        if (delta.IsZero)
        {
            return;
        }

        s.Str += delta.Str;
        s.Dex += delta.Dex;
        s.Agi += delta.Agi;
        s.Int += delta.Int;
        s.Vit += delta.Vit;
        s.ManaRegen += delta.ManaRegen;
        bonuses.Applied = desired;
        if (delta.HasAttributes)
        {
            Leveling.RefreshDerived(world, e);
        }
    }

    /// <summary>
    /// Depois de refazer as estatísticas do zero (equipamento, <see cref="Items.ItemMath.Recompute"/>), o que vinha de status
    /// saiu delas: zera a contabilidade e soma de novo o dos status ativos.
    /// </summary>
    public static void AfterRecompute(World world, Entity e)
    {
        if (e.TryGet<StatusStatBonuses>() is { } bonuses)
        {
            bonuses.Applied = StatBlock.Zero;
            Sync(world, e);
        }
    }
}
