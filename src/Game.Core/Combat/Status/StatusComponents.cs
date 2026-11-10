using System.Numerics;
using Game.Core.Simulation;

namespace Game.Core.Combat.Status;

/// <summary>Um status ativo em uma entidade. Ativo enquanto <c>world.Tick &lt; ExpiresAtTick</c>.</summary>
public sealed class StatusInstance
{
    /// <summary>Id único por entidade (cresce a cada status novo).</summary>
    public int Id { get; init; }
    public string StatusId { get; init; } = "";

    /// <summary>Quem aplicou por último (fonte dos efeitos periódicos). None se sem fonte.</summary>
    public EntityId Source { get; set; }
    public long AppliedAtTick { get; set; }
    public long ExpiresAtTick { get; set; }
    public int Stacks { get; set; } = 1;

    /// <summary>Próximo tick de cada entrada de <c>Periodic</c> (mesma ordem da definição).</summary>
    public long[] NextPeriodicTick { get; set; } = [];

    /// <summary>Dano recebido desde a aplicação (para <c>breaks.damageFraction</c>, T-341). Renovar o status zera.</summary>
    public float DamageTaken { get; set; }

    /// <summary>
    /// Ponto de origem de quem aplicou (T-351, medo: o alvo foge dele): o centro do telegráfo/área ou a posição da fonte na
    /// aplicação. Renovar o status atualiza. Null = sem origem (sem fonte nem ponto).
    /// </summary>
    public Vector2? Origin { get; set; }

    public StatusInstance Clone() => new()
    {
        Id = Id,
        StatusId = StatusId,
        Source = Source,
        AppliedAtTick = AppliedAtTick,
        ExpiresAtTick = ExpiresAtTick,
        Stacks = Stacks,
        NextPeriodicTick = (long[])NextPeriodicTick.Clone(),
        DamageTaken = DamageTaken,
        Origin = Origin,
    };
}

/// <summary>Status ativos da entidade e o estado do retorno decrescente de controle.</summary>
public sealed class StatusEffects : IComponent, IReplicated
{
    public List<StatusInstance> Active { get; } = new();
    public int NextInstanceId { get; set; } = 1;

    /// <summary>Aplicações de controle dentro da janela atual (retorno decrescente).</summary>
    public int ControlCount { get; set; }

    /// <summary>Fim da janela de retorno decrescente (tick absoluto); só vale se ControlCount &gt; 0.</summary>
    public long ControlWindowEndTick { get; set; }

    public IComponent Clone()
    {
        var c = new StatusEffects
        {
            NextInstanceId = NextInstanceId,
            ControlCount = ControlCount,
            ControlWindowEndTick = ControlWindowEndTick,
        };
        c.Active.AddRange(Active.Select(a => a.Clone()));
        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(NextInstanceId);
        hasher.Add(ControlCount);
        hasher.Add(ControlWindowEndTick);
        foreach (var a in Active)
        {
            hasher.Add(a.Id);
            hasher.Add(a.StatusId);
            hasher.Add(a.Source.Value);
            hasher.Add(a.AppliedAtTick);
            hasher.Add(a.ExpiresAtTick);
            hasher.Add(a.Stacks);
            hasher.Add(a.DamageTaken);
            hasher.Add(a.Origin is { } o ? 1 : 0);
            if (a.Origin is { } origin)
            {
                hasher.Add(origin.X);
                hasher.Add(origin.Y);
            }

            foreach (var n in a.NextPeriodicTick)
            {
                hasher.Add(n);
            }
        }
    }
}
