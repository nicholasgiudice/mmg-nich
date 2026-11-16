using Game.Core.Combat.Items;

namespace MmoRpgClient.Hud;

/// <summary>
/// Contas da quantidade na loja (T-384), sem Godot para serem testadas: o teto do campo, o "Máx." e o texto digitado.
/// Só sugerem: quem decide a compra e a venda é a autoridade (<c>Shop.Buy</c>/<c>Shop.Sell</c>).
/// </summary>
public static class ShopQuantity
{
    /// <summary>Unidades de <paramref name="item"/> que ainda cabem na mochila (pilhas iguais com folga e espaços vazios).</summary>
    public static int Room(Inventory? inventory, ItemInstance item, int maxStack)
    {
        if (inventory is null)
        {
            return 0;
        }

        var room = 0;
        foreach (var slot in inventory.Slots)
        {
            room += slot is null ? maxStack : SameStack(slot, item) ? Math.Max(0, maxStack - slot.Count) : 0;
        }

        return room;
    }

    /// <summary>Teto do campo na compra: o limite da loja e o espaço na mochila (nunca menos de 1).</summary>
    public static int BuyLimit(int shopLimit, int room) => Math.Max(1, Math.Min(shopLimit, room));

    /// <summary>"Máx." na compra: o que cabe no ouro, no espaço da mochila e no limite da loja (nunca menos de 1).</summary>
    public static int BuyMax(int price, long gold, int room, int shopLimit)
    {
        var affordable = price <= 0 ? shopLimit : (int)Math.Min(shopLimit, Math.Max(0, gold) / price);
        return Math.Max(1, Math.Min(affordable, BuyLimit(shopLimit, room)));
    }

    /// <summary>Preço total (preço × quantidade).</summary>
    public static long Total(int price, int quantity) => (long)price * quantity;

    /// <summary>Quantidade entre 1 e <paramref name="max"/>.</summary>
    public static int Clamp(int quantity, int max) => Math.Clamp(quantity, 1, Math.Max(1, max));

    /// <summary>
    /// Texto digitado no campo: só os dígitos contam, e o número fica entre 1 e <paramref name="max"/>. Campo vazio ou sem
    /// número devolve null (o jogador ainda está digitando; a quantidade anterior continua valendo).
    /// </summary>
    public static int? Parse(string text, int max)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        return int.TryParse(digits, out var value) ? Clamp(value, max) : Math.Max(1, max);
    }

    private static bool SameStack(ItemInstance a, ItemInstance b) =>
        a.BaseId == b.BaseId && a.Level == b.Level && a.Rarity == b.Rarity && a.Affixes.SequenceEqual(b.Affixes);
}
