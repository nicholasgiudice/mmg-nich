using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat;

/// <summary>
/// Pode a entidade agir? Ponto de extensão (T-009: stun/silence). Retorna o motivo da rejeição ou null se permite.
/// Vários gates podem coexistir: o primeiro que rejeitar vence (ordem de registro).
/// Chamado na validação (início da conjuração) e de novo ao concluir a conjuração.
/// </summary>
public interface IActionGate
{
    /// <param name="ability">Null se a habilidade não existe (o gate ainda é consultado antes de "desconhecida").</param>
    AbilityRejectReason? CanUse(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack);
}

/// <summary>
/// Multiplicadores fornecidos por outros sistemas (T-009: status). Todos opcionais (padrão = 1);
/// os valores de todas as fontes são multiplicados.
/// </summary>
public interface IModifierSource
{
    /// <summary>Dano causado pela <paramref name="source"/>.</summary>
    double DamageDealtMultiplier(World world, Entity source, Entity target, DamageSchool school) => 1.0;

    /// <summary>Dano recebido por <paramref name="target"/> (source pode ser null: dano sem fonte).</summary>
    double DamageTakenMultiplier(World world, Entity target, Entity? source, DamageSchool school) => 1.0;

    /// <summary>Velocidade de movimento (0 = enraizado/atordoado). Consumido pelo MovementSystem.</summary>
    double SpeedMultiplier(World world, Entity entity) => 1.0;
}

/// <summary>Registros de combate do mundo: efeitos por tipo, gates e fontes de modificadores. Montado no <see cref="Simulation.WorldBuilder"/>.</summary>
public sealed class CombatServices
{
    public EffectRegistry Effects { get; } = new();

    public List<IActionGate> Gates { get; } = new();

    public List<IModifierSource> Modifiers { get; } = new();

    public AbilityRejectReason? CheckGates(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack)
    {
        foreach (var g in Gates)
        {
            var r = g.CanUse(world, actor, abilityId, ability, isBasicAttack);
            if (r is not null)
            {
                return r;
            }
        }

        return null;
    }

    public double DamageDealtMultiplier(World world, Entity source, Entity target, DamageSchool school)
    {
        var m = 1.0;
        foreach (var s in Modifiers)
        {
            m *= s.DamageDealtMultiplier(world, source, target, school);
        }

        return m;
    }

    public double DamageTakenMultiplier(World world, Entity target, Entity? source, DamageSchool school)
    {
        var m = 1.0;
        foreach (var s in Modifiers)
        {
            m *= s.DamageTakenMultiplier(world, target, source, school);
        }

        return m;
    }

    public double SpeedMultiplier(World world, Entity entity)
    {
        var m = 1.0;
        foreach (var s in Modifiers)
        {
            m *= s.SpeedMultiplier(world, entity);
        }

        return Math.Max(0, m);
    }
}

/// <summary>
/// ÚNICO lugar onde módulos de combate se registram (efeitos, gates, modificadores).
/// Cada tarefa cria seu módulo em sua própria pasta e ocupa o SEU slot abaixo (linhas separadas
/// por comentários para não conflitar no merge).
/// </summary>
public static class CombatModules
{
    public static void Register(CombatServices s)
    {
        CoreEffects.Register(s); // T-008: damage, heal, shield, displacement, interrupt

        // ---- slot T-009 (status): efeitos apply_status/remove_status, IActionGate, IModifierSource ----
        Status.StatusModule.Register(s);

        // ---- slot T-010 (recursos): efeito resource ----
        Resources.ResourceModule.Register(s);

        // ---- integração status -> recursos (consulta de tags de status) ----
        Integration.StatusResourceBridge.Register(s);

        // ---- slot T-011 (IA/ameaça): efeito threat ----

        // ---- slot T-012 (áreas/invocação/fases): efeitos spawn_area/summon ----
    }
}
