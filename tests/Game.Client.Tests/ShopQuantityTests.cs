using Game.Core.Combat.Items;
using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>T-384: contas da quantidade na loja (teto do campo, "Máx.", total e texto digitado).</summary>
public class ShopQuantityTests
{
    private const string Potion = "consumable.minor_healing_potion";

    [Fact]
    public void Room_counts_free_space_in_matching_stacks_and_empty_slots()
    {
        var inv = new Inventory(3);
        inv[0] = ItemInstance.Stack(Potion, 15);
        inv[1] = ItemInstance.Stack("consumable.minor_mana_potion", 20);

        // 5 na pilha de cura + 20 no espaço vazio; a pilha de mana não conta.
        Assert.Equal(25, ShopQuantity.Room(inv, ItemInstance.Stack(Potion, 1), maxStack: 20));
        Assert.Equal(0, ShopQuantity.Room(null, ItemInstance.Stack(Potion, 1), maxStack: 20));
    }

    [Theory]
    [InlineData(15, 75, 100, 100, 5)] // o ouro limita: 75 / 15
    [InlineData(15, 10_000, 8, 100, 8)] // a mochila limita
    [InlineData(15, 10_000, 600, 100, 100)] // o limite da loja limita
    [InlineData(15, 0, 100, 100, 1)] // sem ouro: fica em 1 (o total aparece em vermelho)
    [InlineData(15, 75, 0, 100, 1)] // mochila cheia: fica em 1 (a autoridade recusa)
    public void Max_fits_gold_room_and_shop_limit(int price, long gold, int room, int shopLimit, int expected) =>
        Assert.Equal(expected, ShopQuantity.BuyMax(price, gold, room, shopLimit));

    [Fact]
    public void Buy_limit_is_the_shop_limit_or_the_room_but_at_least_one()
    {
        Assert.Equal(20, ShopQuantity.BuyLimit(shopLimit: 100, room: 20));
        Assert.Equal(100, ShopQuantity.BuyLimit(shopLimit: 100, room: 600));
        Assert.Equal(1, ShopQuantity.BuyLimit(shopLimit: 100, room: 0));
    }

    [Fact]
    public void Total_is_price_times_quantity()
    {
        Assert.Equal(75, ShopQuantity.Total(15, 5));
        Assert.Equal(15, ShopQuantity.Total(15, 1));
    }

    [Theory]
    [InlineData("4", 20, 4)]
    [InlineData("12", 20, 12)]
    [InlineData("250", 20, 20)] // acima do teto: o teto
    [InlineData("0", 20, 1)] // abaixo de 1: 1
    [InlineData("a3b", 20, 3)] // só os dígitos contam
    [InlineData("99999999999", 20, 20)] // número grande demais: o teto
    public void Typed_text_becomes_a_quantity_between_one_and_the_limit(string text, int limit, int expected) =>
        Assert.Equal(expected, ShopQuantity.Parse(text, limit));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Text_without_a_number_keeps_the_previous_quantity(string text) =>
        Assert.Null(ShopQuantity.Parse(text, 20));

    [Fact]
    public void Clamp_keeps_the_quantity_between_one_and_the_limit()
    {
        Assert.Equal(1, ShopQuantity.Clamp(0, 20));
        Assert.Equal(20, ShopQuantity.Clamp(21, 20));
        Assert.Equal(1, ShopQuantity.Clamp(5, 0));
    }
}
