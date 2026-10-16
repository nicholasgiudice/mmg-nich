using System.Runtime.CompilerServices;
using Game.Core.Definitions;

namespace Game.Bots;

/// <summary>Papel genérico de uma habilidade, deduzido dos efeitos (sem depender de ids).</summary>
public enum AbilityRole
{
    Damage,
    Heal,
    Defensive,
    Taunt,
    Dash,
    Interrupt,
    Other,

    /// <summary>Remove status (T-018): Purificação.</summary>
    Cleanse,
}

/// <summary>Classificação de uma habilidade a partir da definição em dados. Permite estratégias genéricas guiadas por dados.</summary>
public sealed class AbilityInfo
{
    private static readonly ConditionalWeakTable<AbilityDefinition, AbilityInfo> Cache = new();

    private AbilityInfo(AbilityDefinition def)
    {
        Definition = def;
        // T-017: efeitos com "target": "self" (ex.: cura própria da Corrente Sagrada) não definem o papel da habilidade.
        var effects = def.Effects.Where(e => e.Target == EffectTarget.Targets).ToList();
        DamageTotal = effects.OfType<DamageEffect>().Sum(e => e.Base);
        HasHeal = effects.OfType<HealEffect>().Any();
        HasShield = effects.OfType<ShieldEffect>().Any();
        HasTaunt = effects.OfType<ThreatEffect>().Any(t => t.TauntDurationTicks is not null);
        HasInterrupt = effects.OfType<InterruptEffect>().Any();
        HasStatus = effects.OfType<ApplyStatusEffect>().Any();
        CleanseTags = effects.OfType<RemoveStatusEffect>()
            .SelectMany(r => r.Tags.Count > 0 ? r.Tags : (r.Tag.Length > 0 ? new[] { r.Tag } : Array.Empty<string>())).ToList();
        Dash = effects.OfType<DisplacementEffect>().FirstOrDefault(d => d.Kind == DisplacementKind.Dash);
        Targeting = def.Targeting;
        Reach = def.Targeting switch
        {
            UnitTargeting u => u.Range,
            MeleeArcTargeting m => m.Range,
            LineTargeting l => l.Length,
            CircleAtPointTargeting c => c.Range,
            CircleSelfTargeting c => c.Radius,
            _ => 0,
        };
        AreaRadius = def.Targeting switch
        {
            CircleAtPointTargeting c => c.Radius,
            CircleSelfTargeting c => c.Radius,
            _ => 0,
        };
        IsAoe = def.Targeting is CircleAtPointTargeting or CircleSelfTargeting or LineTargeting;
        NeedsUnitTarget = def.Targeting is UnitTargeting;
        TargetsAllies = def.Targeting is UnitTargeting { Faction: FactionFilter.Ally or FactionFilter.Any } && DamageTotal <= 0;
        IsSelfCentered = def.Targeting is SelfTargeting or CircleSelfTargeting;

        var dealsDamage = DamageTotal > 0;
        Role = (HasTaunt, HasHeal, dealsDamage) switch
        {
            (true, _, false) => AbilityRole.Taunt,
            (_, true, _) => AbilityRole.Heal,
            _ when HasInterrupt && !dealsDamage => AbilityRole.Interrupt,
            _ when Dash is not null && !dealsDamage => AbilityRole.Dash,
            _ when CleanseTags.Count > 0 && !dealsDamage => AbilityRole.Cleanse,
            _ when !dealsDamage && (HasShield || HasStatus) && (IsSelfCentered || TargetsAllies) => AbilityRole.Defensive,
            _ when dealsDamage => AbilityRole.Damage,
            _ => AbilityRole.Other,
        };
    }

    public AbilityDefinition Definition { get; }
    public string Id => Definition.Id;
    public AbilityRole Role { get; }
    public double DamageTotal { get; }
    public bool HasHeal { get; }
    public bool HasShield { get; }
    public bool HasTaunt { get; }
    public bool HasInterrupt { get; }
    public bool HasStatus { get; }
    public IReadOnlyList<string> CleanseTags { get; }
    public DisplacementEffect? Dash { get; }
    public TargetingDefinition Targeting { get; }

    /// <summary>Alcance útil (tiles, da borda do alvo): alcance da unidade/arco/linha/ponto, raio de círculo próprio, 0 para self.</summary>
    public double Reach { get; }
    public double AreaRadius { get; }
    public bool IsAoe { get; }
    public bool NeedsUnitTarget { get; }
    public bool TargetsAllies { get; }
    public bool IsSelfCentered { get; }

    /// <summary>Conjuração parada: tem tempo de conjuração e não pode andar enquanto conjura.</summary>
    public bool NeedsStanding => Definition.CastTimeTicks > 0 && !Definition.CastWhileMoving;

    public bool IsBackwardDash =>
        Dash is { Direction: DisplacementDirection.Backward or DisplacementDirection.AwayFromSource };

    public static AbilityInfo For(AbilityDefinition def) => Cache.GetValue(def, d => new AbilityInfo(d));

    /// <summary>Seletor: id exato da habilidade ou <c>tag:nome</c>.</summary>
    public bool Matches(IEnumerable<string> selectors)
    {
        foreach (var s in selectors)
        {
            if (s.StartsWith("tag:", StringComparison.Ordinal))
            {
                if (Definition.Tags.Contains(s[4..]))
                {
                    return true;
                }
            }
            else if (s == Id)
            {
                return true;
            }
        }

        return false;
    }
}
