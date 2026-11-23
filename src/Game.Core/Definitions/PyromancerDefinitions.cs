namespace Game.Core.Definitions;

/// <summary>
/// Efeitos só em quem tem um estado (T-441, Combustão). JSON: <c>if_status</c>. Entre os alvos do uso, os que têm, <b>no
/// momento do uso</b>, um status ativo com qualquer uma das <see cref="Tags"/> recebem os <see cref="Effects"/> (com a mesma
/// fonte, habilidade e potência); os outros, nada. Com <see cref="Consume"/>, depois dos efeitos, esses status são tirados de
/// cada um. Quem passa a ter o status durante o uso (por uma cópia, por exemplo) não conta.
/// </summary>
public sealed record IfStatusEffect : EffectDefinition
{
    /// <summary>Etiquetas dos status procurados (qualquer uma), ex.: <c>["burn"]</c>.</summary>
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>Efeitos aplicados em cada alvo que tem o status.</summary>
    public required IReadOnlyList<EffectDefinition> Effects { get; init; }

    /// <summary>Tira os status com as <see cref="Tags"/> de cada alvo atingido, depois dos efeitos (Combustão NH1 e NH2).</summary>
    public bool Consume { get; init; }
}

/// <summary>
/// Grupo de status que não convivem numa entidade (T-441, Queimadura por NH). Aplicar um status do grupo quando a entidade já
/// tem outro do mesmo grupo: fica o de <see cref="Tier"/> mais alto (empate: o novo), e a duração é a maior entre o que falta
/// do antigo e a do novo. O que fica não troca de fonte nem de números se for o antigo. Reaplicar o mesmo status também não
/// encurta a duração.
/// </summary>
public sealed record StatusExclusiveDefinition
{
    /// <summary>Nome do grupo, ex.: <c>pyromancer_burn</c>.</summary>
    public required string Group { get; init; }

    /// <summary>Força dentro do grupo (a Queimadura usa o NH: 1 a 5). Maior ganha.</summary>
    public required int Tier { get; init; }
}
