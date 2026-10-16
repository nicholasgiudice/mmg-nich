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

    public StatusInstance Clone() => new()
    {
        Id = Id,
        StatusId = StatusId,
        Source = Source,
        AppliedAtTick = AppliedAtTick,
        ExpiresAtTick = ExpiresAtTick,
        Stacks = Stacks,
        NextPeriodicTick = (long[])NextPeriodicTick.Clone(),
    };
}

/// <summary>Status ativos da entidade e o estado do retorno decrescente de controle.</summary>
public sealed class StatusEffects : IComponent
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
            foreach (var n in a.NextPeriodicTick)
            {
                hasher.Add(n);
            }
        }
    }
}
