using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat;

/// <summary>Identidade de dados da entidade (para IA, log e regras por tag/rank/resistência).</summary>
public sealed class UnitInfo : IComponent, IReplicated
{
    /// <summary>Id da definição de origem (classe ou monstro), ex.: "class.warrior".</summary>
    public string DefinitionId { get; init; } = "";
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public MonsterRank Rank { get; init; } = MonsterRank.Normal;

    /// <summary>Resistência por escola (chave = nome snake_case, ex.: "frost"; 0.25 = -25% de dano).</summary>
    public IReadOnlyDictionary<string, double> Resistances { get; init; } = new Dictionary<string, double>();

    public bool HasTag(string tag) => Tags.Contains(tag);

    public double ResistanceTo(DamageSchool school) =>
        Resistances.TryGetValue(SchoolName(school), out var r) ? r : 0;

    public static string SchoolName(DamageSchool s) => s switch
    {
        DamageSchool.Physical => "physical",
        DamageSchool.Arcane => "arcane",
        DamageSchool.Fire => "fire",
        DamageSchool.Frost => "frost",
        DamageSchool.Holy => "holy",
        DamageSchool.Nature => "nature",
        _ => s.ToString().ToLowerInvariant(),
    };

    // Imutável depois do spawn: o clone pode compartilhar a instância.
    public IComponent Clone() => this;

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(DefinitionId);
        hasher.Add((int)Rank);
        foreach (var kv in Resistances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hasher.Add(kv.Key);
            hasher.Add((float)kv.Value);
        }
    }
}

/// <summary>Conjuração em andamento.</summary>
public sealed class CastState
{
    public required string AbilityId { get; init; }
    public long StartTick { get; init; }

    /// <summary>A conjuração conclui no primeiro tick com <c>world.Tick >= EndTick</c>.</summary>
    public long EndTick { get; init; }
    public EntityId? Target { get; init; }
    public Vector2? AimPoint { get; init; }

    /// <summary>True se a conjuração ligou <c>Movement.LockFacing</c> (e deve desligá-lo ao terminar).</summary>
    public bool LockedFacing { get; set; }

    public CastState Clone() => new()
    {
        AbilityId = AbilityId,
        StartTick = StartTick,
        EndTick = EndTick,
        Target = Target,
        AimPoint = AimPoint,
        LockedFacing = LockedFacing,
    };
}

/// <summary>Habilidades conhecidas, recargas (em tick absoluto) e conjuração atual.</summary>
public sealed class AbilityState : IComponent, IReplicated
{
    public List<string> Known { get; } = new();

    /// <summary>Id do ataque básico (tem recarga própria; é o que "velocidade de ataque" ajusta). Null = nenhum.</summary>
    public string? BasicAttackId { get; set; }

    /// <summary>Tick a partir do qual a habilidade está pronta (ausente = pronta).</summary>
    public SortedDictionary<string, long> ReadyAtTick { get; } = new(StringComparer.Ordinal);

    public CastState? Cast { get; set; }

    public bool IsCasting => Cast is not null;

    /// <summary>
    /// Consumíveis (T-017, <c>ClassDefinition.Consumables</c>): conhecidos e usáveis como qualquer habilidade, mas separados de
    /// <see cref="Known"/> (que lista só as habilidades de classe) para cliente e bots distinguirem.
    /// </summary>
    public List<string> Consumables { get; } = new();

    /// <summary>Cargas já gastas por habilidade (ausente = 0). Restantes = <c>AbilityDefinition.Charges</c> - gastas.</summary>
    public SortedDictionary<string, int> ChargesUsed { get; } = new(StringComparer.Ordinal);

    // Criados sob demanda: o protótipo (tudo no NH1, sem cargas por recarga) não paga a alocação a cada snapshot.
    private SortedDictionary<string, int>? _ranks;
    private SortedDictionary<string, int>? _cooldownChargesSpent;

    /// <summary>NH de cada habilidade (T-060). Ausente = NH1.</summary>
    public SortedDictionary<string, int> Ranks => _ranks ??= new(StringComparer.Ordinal);

    /// <summary>
    /// Cargas por recarga em uso (T-060, só habilidades com <c>CooldownCharges</c> &gt; 1): quantas estão recarregando.
    /// A próxima volta em <see cref="ReadyAtTick"/>.
    /// </summary>
    public SortedDictionary<string, int> CooldownChargesSpent => _cooldownChargesSpent ??= new(StringComparer.Ordinal);

    /// <summary>Há cargas por recarga em uso (sem criar o dicionário).</summary>
    public bool HasCooldownChargesSpent => _cooldownChargesSpent is { Count: > 0 };

    public bool Knows(string abilityId) => Known.Contains(abilityId) || Consumables.Contains(abilityId);

    /// <summary>NH da habilidade (T-060); 1 quando não definido.</summary>
    public int RankOf(string abilityId) => _ranks is not null && _ranks.TryGetValue(abilityId, out var r) ? r : 1;

    /// <summary>
    /// Em recarga para usar agora? Com cargas por recarga, só quando todas estão recarregando; senão, pela
    /// recarga normal.
    /// </summary>
    public bool IsOnCooldown(AbilityDefinition ability, long currentTick) =>
        ability.CooldownCharges > 1
            ? _cooldownChargesSpent is not null && _cooldownChargesSpent.TryGetValue(ability.Id, out var spent) && spent >= ability.CooldownCharges
            : CooldownRemaining(ability.Id, currentTick) > 0;

    /// <summary>Cargas prontas para usar agora (1 ou 0 para habilidades sem cargas por recarga).</summary>
    public int CooldownChargesReady(AbilityDefinition ability, long currentTick) =>
        ability.CooldownCharges > 1
            ? ability.CooldownCharges - (_cooldownChargesSpent is not null && _cooldownChargesSpent.TryGetValue(ability.Id, out var spent) ? spent : 0)
            : CooldownRemaining(ability.Id, currentTick) > 0 ? 0 : 1;

    /// <summary>Usos restantes da habilidade, ou null se ilimitada.</summary>
    public int? ChargesRemaining(AbilityDefinition ability) =>
        ability.Charges is { } max ? Math.Max(0, max - (ChargesUsed.TryGetValue(ability.Id, out var u) ? u : 0)) : null;

    public bool IsBasicAttack(string abilityId) => BasicAttackId == abilityId;

    public long CooldownRemaining(string abilityId, long currentTick) =>
        ReadyAtTick.TryGetValue(abilityId, out var t) ? Math.Max(0, t - currentTick) : 0;

    public IComponent Clone()
    {
        var c = new AbilityState { BasicAttackId = BasicAttackId, Cast = Cast?.Clone() };
        c.Known.AddRange(Known);
        c.Consumables.AddRange(Consumables);
        foreach (var kv in ChargesUsed)
        {
            c.ChargesUsed[kv.Key] = kv.Value;
        }

        foreach (var kv in ReadyAtTick)
        {
            c.ReadyAtTick[kv.Key] = kv.Value;
        }

        if (_ranks is { Count: > 0 })
        {
            c._ranks = new SortedDictionary<string, int>(_ranks, StringComparer.Ordinal);
        }

        if (_cooldownChargesSpent is { Count: > 0 })
        {
            c._cooldownChargesSpent = new SortedDictionary<string, int>(_cooldownChargesSpent, StringComparer.Ordinal);
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(BasicAttackId ?? "");
        foreach (var k in Known)
        {
            hasher.Add(k);
        }

        foreach (var kv in ReadyAtTick)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }

        foreach (var k in Consumables)
        {
            hasher.Add(k);
        }

        foreach (var kv in ChargesUsed)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }

        foreach (var kv in _ranks ?? [])
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }

        foreach (var kv in _cooldownChargesSpent ?? [])
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }

        hasher.Add(Cast is not null);
        if (Cast is { } cs)
        {
            hasher.Add(cs.AbilityId);
            hasher.Add(cs.StartTick);
            hasher.Add(cs.EndTick);
            hasher.Add(cs.Target?.Value ?? 0);
            hasher.Add(cs.AimPoint.HasValue);
            hasher.Add(cs.AimPoint ?? Vector2.Zero);
            hasher.Add(cs.LockedFacing);
        }
    }
}

public sealed class ShieldInstance
{
    public int Id { get; init; }
    public float Remaining { get; set; }
    public long ExpiresAtTick { get; init; }
    public EntityId Source { get; init; }
    public string SourceId { get; init; } = "";
}

/// <summary>Escudos ativos (absorvem na ordem de criação, antes da vida).</summary>
public sealed class ShieldState : IComponent, IReplicated
{
    public List<ShieldInstance> Shields { get; } = new();
    public int NextShieldId { get; set; } = 1;

    public float Total => Shields.Sum(s => s.Remaining);

    public IComponent Clone()
    {
        var c = new ShieldState { NextShieldId = NextShieldId };
        foreach (var s in Shields)
        {
            c.Shields.Add(new ShieldInstance
            {
                Id = s.Id,
                Remaining = s.Remaining,
                ExpiresAtTick = s.ExpiresAtTick,
                Source = s.Source,
                SourceId = s.SourceId,
            });
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(NextShieldId);
        foreach (var s in Shields)
        {
            hasher.Add(s.Id);
            hasher.Add(s.Remaining);
            hasher.Add(s.ExpiresAtTick);
            hasher.Add(s.Source.Value);
        }
    }
}
