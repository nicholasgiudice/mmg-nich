namespace Game.Core.Definitions;

/// <summary>
/// Canalização com pulsos (T-442). Ao usar, a habilidade paga o custo e entra na recarga; depois, enquanto quem lança
/// canaliza, os efeitos da habilidade saem no alvo a cada <see cref="IntervalTicks"/> (o pulso n no tick
/// início + n × intervalo; o último fecha a canalização). Andar (sem <see cref="MoveSpeedMultiplier"/>), ser atordoado,
/// silenciado, interrompido ou morrer corta a canalização, e os pulsos que faltam não saem.
/// </summary>
public sealed record ChannelDefinition
{
    /// <summary>Quantos pulsos (pelo menos 1).</summary>
    public required int Pulses { get; init; }

    /// <summary>Tempo entre pulsos (JSON "interval", segundos; pelo menos 1 tick).</summary>
    public required int IntervalTicks { get; init; }

    /// <summary>
    /// Quanto cada pulso causa a mais que o anterior, acumulando (Raio Arcano: 0,15): o pulso n sai com a potência
    /// × (1 + pulseGrowth)^(n−1). Vale para dano, cura e escudo. 0 = todos iguais.
    /// </summary>
    public double PulseGrowth { get; init; }

    /// <summary>
    /// Se definido, quem lança pode andar canalizando, com a velocidade × este número (Raio Arcano NH5 e Feixe do Vazio:
    /// 0,5). Null = andar corta a canalização. Em (0, 1].
    /// </summary>
    public double? MoveSpeedMultiplier { get; init; }

    /// <summary>
    /// Efeitos no alvo quando a canalização vai até o fim, depois do último pulso (Feixe do Vazio NH3: +15% de dano
    /// recebido por 6 s). Cortada no meio, não saem.
    /// </summary>
    public IReadOnlyList<EffectDefinition> OnComplete { get; init; } = [];
}

/// <summary>
/// Disparos no alvo atual (T-442, Orbes Arcanos). Pensado para o efeito periódico de um status em quem lança: aplica
/// <see cref="Effects"/> <see cref="Count"/> vezes (um disparo por orbe) no alvo atual de quem recebe este efeito (o
/// último alvo de unidade que ele escolheu ao usar uma habilidade), com a fonte do efeito (crítico, log e ameaça dela).
/// Sem alvo atual, alvo morto, fora de <see cref="Range"/>, sem linha de visão ou de outra facção, nada sai.
/// </summary>
public sealed record CurrentTargetEffect : EffectDefinition
{
    /// <summary>Alcance a partir de quem tem o alvo atual (tiles).</summary>
    public required double Range { get; init; }

    /// <summary>Disparos por vez (orbes), pelo menos 1.</summary>
    public int Count { get; init; } = 1;

    /// <summary>Facção do alvo atual em relação à fonte.</summary>
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;
    public required IReadOnlyList<EffectDefinition> Effects { get; init; }
}
