using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat;

/// <summary>
/// Ataque básico trocado por um status (T-422, Ira Divina). O cliente continua mandando a intenção do ataque básico; enquanto
/// quem a manda tem um status com <c>basicAttack</c>, o Core usa a definição daquela habilidade (alvo, efeitos, recarga,
/// conjuração) com o <b>id do ataque básico</b>. Assim a recarga, a divisão pela AGI, os eventos e o log continuam sendo do
/// ataque básico e nada novo vai para a rede. Quando o status acaba, o próximo uso volta à definição normal. A habilidade
/// trocada usa o NH1 (um status por NH, se os números mudam). Habilidade com telegráfo não é suportada na troca.
/// </summary>
public static class BasicAttackOverride
{
    /// <summary>
    /// A definição a usar para <paramref name="abilityId"/>: a trocada, se é o ataque básico de quem usa e há um status com
    /// <c>basicAttack</c> ativo; senão <paramref name="ability"/> sem mudança.
    /// </summary>
    public static AbilityDefinition? Resolve(World world, Entity caster, AbilityState? st, string abilityId, AbilityDefinition? ability)
    {
        if (ability is null || st is null || !st.IsBasicAttack(abilityId) || Active(world, caster) is not { } id
            || world.Content.AbilityAt(id, 1) is not { } swapped)
        {
            return ability;
        }

        return swapped with { Id = abilityId };
    }

    /// <summary>Habilidade do status com <c>basicAttack</c> aplicado por último (null = nenhum).</summary>
    public static string? Active(World world, Entity entity)
    {
        string? id = null;
        foreach (var s in Status.StatusManager.Active(world, entity))
        {
            if (Status.StatusManager.Def(world, s).BasicAttack is { } b)
            {
                id = b;
            }
        }

        return id;
    }
}
