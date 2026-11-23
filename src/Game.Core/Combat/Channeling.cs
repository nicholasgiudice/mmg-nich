using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat;

/// <summary>
/// Andamento de uma canalização (T-442): potência do uso e pulsos que já saíram. Estado interno do servidor: o cliente
/// vê a canalização pelo <see cref="AbilityState.Cast"/> do snapshot (início e fim) e cada pulso pelo <see cref="AbilityHit"/>.
/// Vale só enquanto casa com a conjuração atual (<see cref="Matches"/>); cortada a canalização, fica parado até o próximo uso.
/// </summary>
public sealed class ChannelProgress : IComponent
{
    public string AbilityId { get; set; } = "";
    public long StartTick { get; set; }
    public int PulsesDone { get; set; }
    public double Power { get; set; } = 1;

    public bool Matches(CastState cast) => cast.AbilityId == AbilityId && cast.StartTick == StartTick;

    public IComponent Clone() => new ChannelProgress { AbilityId = AbilityId, StartTick = StartTick, PulsesDone = PulsesDone, Power = Power };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(AbilityId);
        hasher.Add(StartTick);
        hasher.Add(PulsesDone);
        hasher.Add((float)Power);
    }
}

/// <summary>
/// Canalização com pulsos (T-442, <see cref="ChannelDefinition"/>). A canalização usa a mesma <see cref="CastState"/> da
/// conjuração (fim = último pulso), então tudo que corta uma conjuração corta a canalização: atordoar, silenciar, medo,
/// <c>interrupt</c>, morrer e andar (salvo com <c>moveSpeedMultiplier</c>). Os pulsos não são revalidados como um uso novo
/// (custo e recarga já foram no início): cada um resolve o alvo de novo e, fora de alcance ou sem linha de visão, o pulso
/// se perde. Alvo de unidade morto ou sumido encerra a canalização sem <see cref="CastInterrupted"/>.
/// </summary>
public static class Channeling
{
    public static void Register(CombatServices s) => s.Modifiers.Add(new ChannelSpeed()); // andar devagar canalizando

    /// <summary>Começa a canalização (chamado pelo <c>AbilitySystem</c> depois de pagar o custo e pôr a recarga).</summary>
    internal static void Begin(World world, Entity caster, AbilityState st, AbilityDefinition ability, EntityId? target, Vector2? aim, double power)
    {
        var channel = ability.Channel!;
        st.Cast = new CastState
        {
            AbilityId = ability.Id,
            StartTick = world.Tick,
            EndTick = world.Tick + ((long)channel.Pulses * channel.IntervalTicks),
            Target = target,
            AimPoint = aim,
        };

        var progress = caster.TryGet<ChannelProgress>();
        if (progress is null)
        {
            progress = new ChannelProgress();
            caster.Add(progress);
        }

        progress.AbilityId = ability.Id;
        progress.StartTick = world.Tick;
        progress.PulsesDone = 0;
        progress.Power = power;
    }

    /// <summary>Um tick da canalização em andamento: corta ao andar, solta os pulsos devidos e fecha no último.</summary>
    internal static void Advance(World world, Entity caster, AbilityState st, CastState cast, AbilityDefinition ability)
    {
        var channel = ability.Channel!;
        if (caster.TryGet<ChannelProgress>() is not { } progress || !progress.Matches(cast))
        {
            st.Cast = null; // sem andamento (não deveria acontecer): encerra sem pulsos
            return;
        }

        if (channel.MoveSpeedMultiplier is null && caster.TryGet<Movement>() is { } mv && mv.Direction != Vector2.Zero)
        {
            CastControl.Cancel(world, caster, CastInterruptReason.Moved);
            return;
        }

        if (ability.Targeting is UnitTargeting && cast.Target is { } id && world.Find(id) is not { IsAlive: true })
        {
            st.Cast = null; // o alvo morreu ou sumiu: os pulsos que faltam não têm em quem sair
            return;
        }

        while (progress.PulsesDone < channel.Pulses
            && world.Tick >= cast.StartTick + ((long)(progress.PulsesDone + 1) * channel.IntervalTicks))
        {
            progress.PulsesDone++;
            Pulse(world, caster, ability, cast, progress.PulsesDone, channel.Pulses, progress.Power * PulseFactor(channel, progress.PulsesDone));
            if (!caster.IsAlive || st.Cast != cast)
            {
                return; // o pulso cortou a canalização (reflexo que atordoa, morte...)
            }
        }

        if (progress.PulsesDone < channel.Pulses)
        {
            return;
        }

        st.Cast = null;
        if (channel.OnComplete.Count > 0)
        {
            var res = TargetResolver.Resolve(world, caster, ability, cast.Target, cast.AimPoint);
            if (res.Reason is null)
            {
                EffectApplier.Apply(world, channel.OnComplete, caster, res.Targets, ability.Id, ability, cast.AimPoint, res.Direction, progress.Power, channel.Pulses);
            }
        }
    }

    /// <summary>Potência do pulso <paramref name="pulse"/> (1..N) relativa ao primeiro: (1 + pulseGrowth)^(n−1).</summary>
    public static double PulseFactor(ChannelDefinition channel, int pulse) => Math.Pow(1 + channel.PulseGrowth, pulse - 1);

    /// <summary>A entidade está canalizando esta definição agora (para a velocidade)?</summary>
    public static ChannelDefinition? Active(World world, Entity e)
    {
        if (e.TryGet<AbilityState>() is not { Cast: { } cast } st || e.TryGet<ChannelProgress>() is not { } progress || !progress.Matches(cast))
        {
            return null;
        }

        return world.Content.AbilityAt(cast.AbilityId, st.RankOf(cast.AbilityId))?.Channel;
    }

    private static void Pulse(World world, Entity caster, AbilityDefinition ability, CastState cast, int pulse, int count, double power)
    {
        var res = TargetResolver.Resolve(world, caster, ability, cast.Target, cast.AimPoint);
        if (res.Reason is not null)
        {
            return; // fora de alcance ou sem linha de visão: este pulso se perde
        }

        caster.Get<Transform>().Facing = res.Direction;
        world.Events.Publish(new AbilityHit(caster.Id, ability.Id, pulse, count, res.Targets.Select(t => t.Id).ToList()));
        EffectApplier.Apply(world, ability.Effects, caster, res.Targets, ability.Id, ability, cast.AimPoint, res.Direction, power, pulse);
    }

    /// <summary>Andar canalizando (Raio Arcano NH5, Feixe do Vazio): velocidade × <c>moveSpeedMultiplier</c> enquanto canaliza.</summary>
    private sealed class ChannelSpeed : IModifierSource
    {
        public double SpeedMultiplier(World world, Entity entity) =>
            Active(world, entity)?.MoveSpeedMultiplier ?? 1.0;
    }
}
