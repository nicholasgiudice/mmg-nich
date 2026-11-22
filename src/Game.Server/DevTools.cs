using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;

namespace Game.Server;

/// <summary>
/// Ferramenta de teste (T-405, <c>design/segunda-classe-v0.1.md</c> §4.2): o que cada <see cref="DevToolAction"/> faz no
/// personagem. Quem chama (<see cref="ZoneRoom"/>, <c>WorldServer</c>) confere <see cref="ServerOptions.DevTools"/>, responde
/// com <see cref="DevToolResult"/> e grava o personagem quando o resultado é <see cref="DevToolOutcome.Done"/>.
/// </summary>
public static class DevTools
{
    /// <summary>Fonte da XP dada pela ferramenta (<c>XpGained.SourceId</c>; o monstro é <see cref="EntityId.None"/>).</summary>
    public const string ExperienceSource = "dev.tools";

    /// <summary>
    /// Aplica o pedido. Subir de nível dá a XP que falta por <see cref="Leveling.GrantExperience"/>, o mesmo caminho da XP de
    /// monstro (mesmos pontos, vida e eventos <c>LevelUp</c>). Tudo é recusado em combate ou conjurando e com o personagem morto.
    /// </summary>
    public static DevToolOutcome Apply(World world, Entity e, DevToolAction action)
    {
        if (e.TryGet<CharacterProgress>() is not { } progress || e.TryGet<CharacterStats>() is not { } stats)
        {
            return DevToolOutcome.NoProgression;
        }

        if (!e.IsAlive)
        {
            return DevToolOutcome.Dead;
        }

        if (Leveling.InCombat(world, e) || e.TryGet<AbilityState>() is { IsCasting: true })
        {
            return DevToolOutcome.InCombat;
        }

        var rules = world.Content.Rules.Progression;
        switch (action)
        {
            case DevToolAction.UndoSecondClass:
                return Leveling.UndoSecondClass(world, e) ? DevToolOutcome.Done : DevToolOutcome.NoSecondClass;
            case DevToolAction.FreeReset:
                return Leveling.ResetPoints(world, e) is null ? DevToolOutcome.Done : DevToolOutcome.InCombat;
        }

        var target = action switch
        {
            DevToolAction.LevelUp1 => stats.Level + 1,
            DevToolAction.LevelUp5 => stats.Level + 5,
            DevToolAction.ToSecondClassLevel => rules.SecondClassLevel,
            _ => rules.MaxLevel,
        };
        var xp = Leveling.ExperienceToReach(rules, stats.Level, progress.Experience, target);
        if (xp <= 0)
        {
            return DevToolOutcome.AlreadyAtLevel;
        }

        Leveling.GrantExperience(world, e, xp, EntityId.None, ExperienceSource);
        return DevToolOutcome.Done;
    }

    /// <summary>Nível do personagem para o <see cref="DevToolResult"/> (0 sem progressão).</summary>
    public static int LevelOf(Entity? e) => e?.TryGet<CharacterProgress>() is not null && e.TryGet<CharacterStats>() is { } s ? s.Level : 0;
}
