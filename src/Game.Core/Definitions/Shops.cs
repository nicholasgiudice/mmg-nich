namespace Game.Core.Definitions;

/// <summary>
/// Loja de um NPC (T-321, <c>data/shops.json</c>, seção <c>shops</c>): o que o NPC vende, por preço fixo e estoque ilimitado.
/// Toda loja também compra itens do jogador (<see cref="ShopRules.SellRatio"/>).
/// </summary>
public sealed record ShopDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Ofertas, na ordem do arquivo: o índice é o que a intenção de compra manda.</summary>
    public IReadOnlyList<ShopOfferDefinition> Offers { get; init; } = [];
}

/// <summary>
/// Uma oferta: o item (consumível ou equipamento comum, sem afixos) no nível <see cref="Level"/> e o preço de compra por
/// unidade. <see cref="Price"/> ausente = o valor do item (<c>ShopMath.ValueOf</c>: o <c>price</c> do consumível ou a
/// fórmula de equipamento de <see cref="ShopRules"/>).
/// </summary>
public sealed record ShopOfferDefinition
{
    public required string Item { get; init; }
    public int Level { get; init; } = 1;
    public int? Price { get; init; }
}

/// <summary>
/// Regras de loja (T-321, <c>data/shops.json</c>, seção <c>rules.shops</c>). Valor de um equipamento =
/// (<see cref="EquipmentValueBase"/> + <see cref="EquipmentValuePerLevel"/> × nível) × <see cref="EquipmentRarityMultiplier"/>,
/// arredondado; o de um consumível é o <c>price</c> dele. Vender dá <see cref="SellRatio"/> × valor (arredondado para baixo).
/// </summary>
public sealed record ShopRules
{
    /// <summary>Distância máxima (tiles) entre o personagem e o NPC para comprar ou vender.</summary>
    public double InteractRange { get; init; } = 3;

    public double SellRatio { get; init; } = 0.25;
    public double EquipmentValueBase { get; init; } = 10;
    public double EquipmentValuePerLevel { get; init; } = 5;
    public RarityValues EquipmentRarityMultiplier { get; init; } = new() { Common = 1, Uncommon = 2, Rare = 4, Epic = 8 };

    /// <summary>Unidades no máximo por compra ou venda (protege contra pedidos absurdos).</summary>
    public int MaxQuantity { get; init; } = 100;
}
