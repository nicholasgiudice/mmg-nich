namespace Game.Core.Definitions;

/// <summary>
/// Copia estados do alvo para outros perto dele (T-432, Flecha Dispersa; reaproveitado pela Combustão NH5 da T-441). JSON:
/// <c>copy_status</c>. Para cada alvo do efeito, os status ativos dele com qualquer uma das <see cref="Tags"/> (menos os
/// permanentes) vão, com os mesmos acúmulos e o tempo que falta, para até <see cref="Count"/> entidades a até
/// <see cref="Radius"/> tiles do alvo (o corpo conta, como nos alvos extras), as mais perto primeiro (empate: menor id), com
/// linha de visão a partir do alvo. Quem usou o efeito vira a fonte das cópias. Quem já tem o status fica com o maior número
/// de acúmulos e o maior tempo.
/// </summary>
public sealed record CopyStatusEffect : EffectDefinition
{
    /// <summary>Etiquetas dos status copiados (qualquer uma), ex.: <c>["hunter_venom", "bleed"]</c>.</summary>
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>Distância máxima (tiles) do alvo até quem recebe a cópia.</summary>
    public required double Radius { get; init; }

    /// <summary>Quantas entidades recebem a cópia (0 = todas no raio).</summary>
    public int Count { get; init; } = 1;

    /// <summary>Facção de quem recebe, em relação a quem usou (padrão: inimigos).</summary>
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;
}

/// <summary>
/// Status que é escolhido pelo NH de outra habilidade de quem aplica (T-432, Veneno do Caçador pelo NH da Flecha Envenenada).
/// Aplicar o status que tem este campo aplica, no lugar dele, <c>Statuses[NH − 1]</c>, com o NH de <see cref="Ability"/> de
/// quem aplica. Sem pontos na habilidade, sem fonte ou fonte sem NH (monstros): o NH1 (o primeiro da lista). NH acima da
/// lista: o último. O status com <c>byRank</c> nunca fica numa entidade; os outros campos dele não valem.
/// </summary>
public sealed record StatusByRankDefinition
{
    /// <summary>Habilidade cujo NH escolhe o status (ex.: <c>hunter.poisoned_arrow</c>).</summary>
    public required string Ability { get; init; }

    /// <summary>Um status por NH, do NH1 em diante.</summary>
    public required IReadOnlyList<string> Statuses { get; init; }
}

/// <summary>
/// Estado que pula ao acabar (T-432, Praga): enquanto quem tem este status o mantém, quando um status com uma das
/// <see cref="Tags"/> aplicado por ele acaba <b>pelo tempo</b> num inimigo, o mesmo status passa, com todos os acúmulos e a
/// duração padrão, para o inimigo mais perto a até <see cref="Radius"/> tiles (o corpo conta; empate: menor id; com linha de
/// visão) que ainda não tem nenhum status com essas etiquetas. Quem pula continua com a mesma fonte, então pode pular de
/// novo enquanto este status dura. Tirado, morto ou acabado, não pula mais.
/// </summary>
public sealed record StatusJumpDefinition
{
    /// <summary>Etiquetas dos status que pulam (qualquer uma), ex.: <c>["hunter_venom"]</c>.</summary>
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>Distância máxima (tiles) do pulo.</summary>
    public required double Radius { get; init; }
}
