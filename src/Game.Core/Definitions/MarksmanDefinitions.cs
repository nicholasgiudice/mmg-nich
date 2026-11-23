namespace Game.Core.Definitions;

/// <summary>
/// Crítico vindo de um status de quem ataca (T-431: Mira Firme, Olho de Falcão). Vale só para dano de habilidade e do ataque
/// básico (efeitos com habilidade de origem); dano periódico de status e de área segue a regra normal de crítico.
/// </summary>
public sealed record StatusCritDefinition
{
    /// <summary>
    /// Chance de crítico a mais (fração, × acúmulos) contra alvos a <see cref="MinDistance"/> tiles ou mais. Soma antes do
    /// teto <c>critMax</c>, como o <c>CritBonus</c> dos afixos. Em [0, 1].
    /// </summary>
    public double Bonus { get; init; }

    /// <summary>
    /// Distância mínima (tiles) até o alvo, medida no acerto como o alcance: do centro de quem ataca à borda do alvo
    /// (distância − raio do alvo). 0 = qualquer distância.
    /// </summary>
    public double MinDistance { get; init; }

    /// <summary>
    /// Multiplicador do crítico contra esses alvos, no lugar de <c>critMultiplier</c> (Mira Firme NH5: 1,65). Null = a regra.
    /// Com vários status, vale o maior.
    /// </summary>
    public double? Multiplier { get; init; }

    /// <summary>
    /// Todo dano de habilidade e do ataque básico de quem tem o status é crítico, acima do teto (Olho de Falcão). Não consome
    /// o RNG. Respeita <see cref="MinDistance"/>.
    /// </summary>
    public bool Guaranteed { get; init; }
}
