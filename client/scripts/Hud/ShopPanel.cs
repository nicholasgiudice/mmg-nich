using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Shops;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Janela da loja de um NPC (T-323; regras na T-321). Aba <b>Comprar</b>: as ofertas de <c>data/shops.json</c> (o índice é o
/// que vai na <see cref="BuyIntent"/>) com ícone, nome, preço e, para consumíveis, a quantidade. Aba <b>Vender</b>: o
/// inventário, com o preço de venda no tooltip e na linha; item raro ou épico pede um segundo clique. Só manda intenções:
/// quem decide é a autoridade, e as recusas voltam como <see cref="ShopRejected"/>. O <c>Hud</c> fecha a janela quando o
/// personagem se afasta do NPC.
/// </summary>
public partial class ShopPanel : ProgressionPanel
{
    private const float ListWidth = 440f;
    private const float ListHeight = 270f;
    private const float IconSize = 40f;
    private const float RowHeight = 46f;
    private const double ConfirmSeconds = 3.0;

    private readonly Dictionary<string, int> _quantities = new(StringComparer.Ordinal);
    private bool _selling;
    private int _confirmSlot = -1;
    private double _confirmLeft;
    private float _scroll;

    /// <summary>NPC da loja aberta; null = fechada.</summary>
    public EntityId? Npc { get; private set; }

    private string _npcName = "";
    private ShopDefinition? _shop;

    /// <summary>Abre a loja do NPC (aba Comprar). Loja que o catálogo não conhece não abre.</summary>
    public bool Open(EntityId npc, NpcInfo info)
    {
        if (info.Shop is not { } shopId || !Content.Shops.TryGetValue(shopId, out var shop))
        {
            return false;
        }

        if (Npc != npc)
        {
            _selling = false;
            _scroll = 0;
            _quantities.Clear();
        }

        Npc = npc;
        _npcName = info.Name;
        _shop = shop;
        _confirmSlot = -1;
        Visible = true;
        SetMessage("");
        return true;
    }

    public void Close()
    {
        Visible = false;
        Npc = null;
        _confirmSlot = -1;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_confirmLeft > 0)
        {
            _confirmLeft -= delta;
            if (_confirmLeft <= 0)
            {
                _confirmSlot = -1;
                Refresh();
            }
        }
    }

    protected override string StateKey(EntitySnapshot self)
    {
        var inv = self.TryGet<Inventory>();
        return string.Join("|", Npc?.Value, _selling, _confirmSlot, self.TryGet<Wallet>()?.Gold,
            inv is null ? "-" : string.Join(";", inv.Slots.Select(i => i is null ? "" : $"{i.BaseId}:{i.Level}:{i.Rarity}:{i.Count}")));
    }

    protected override void Build(VBoxContainer box, EntitySnapshot self)
    {
        if (_shop is null || Npc is null)
        {
            return;
        }

        var header = new HBoxContainer();
        var title = Title(_shop.Name);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var close = new Button { Text = "Fechar (Esc)", FocusMode = FocusModeEnum.None };
        close.AddThemeFontSizeOverride("font_size", 11);
        close.Pressed += Close;
        header.AddChild(close);
        box.AddChild(header);
        box.AddChild(Small($"{_npcName} · compra e venda a até {Content.Rules.Shops.InteractRange:0.#} tiles", 11, 0.6f));

        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 4);
        tabs.AddChild(Tab("Comprar", !_selling, () => SwitchTab(false)));
        tabs.AddChild(Tab("Vender", _selling, () => SwitchTab(true)));
        box.AddChild(tabs);

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 3);
        var gold = self.TryGet<Wallet>()?.Gold ?? 0;
        var classId = self.TryGet<UnitInfo>()?.DefinitionId;
        if (_selling)
        {
            SellRows(list, self, classId);
        }
        else
        {
            BuyRows(list, self, gold, classId);
        }

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(ListWidth, Math.Clamp(list.GetChildCount() * RowHeight, RowHeight, ListHeight)),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        scroll.AddChild(list);
        box.AddChild(scroll);
        // A lista é refeita quando o ouro ou o inventário mudam: volta para onde o jogador estava.
        scroll.Ready += () => scroll.ScrollVertical = (int)_scroll;
        scroll.GetVScrollBar().ValueChanged += v => _scroll = (float)v;

        var footer = new HBoxContainer();
        footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        footer.AddChild(new InventoryPanel.GoldCoin { CustomMinimumSize = new Vector2(14, 14), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var goldLabel = Small("Seu ouro: " + ItemText.Gold(gold), 14, 1f);
        goldLabel.AddThemeColorOverride("font_color", HudTheme.Gold);
        footer.AddChild(goldLabel);
        box.AddChild(footer);
    }

    private void SwitchTab(bool selling)
    {
        if (_selling != selling)
        {
            _selling = selling;
            _scroll = 0;
            _confirmSlot = -1;
            SetMessage("");
        }
    }

    private static Button Tab(string text, bool active, Action pressed)
    {
        var button = new Button { Text = text, ToggleMode = true, ButtonPressed = active, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(90, 0) };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.Pressed += pressed;
        return button;
    }

    private void BuyRows(VBoxContainer list, EntitySnapshot self, long gold, string? classId)
    {
        for (var i = 0; i < _shop!.Offers.Count; i++)
        {
            var offer = _shop.Offers[i];
            if (!Content.Items.TryGetValue(offer.Item, out var def))
            {
                continue;
            }

            var index = i;
            var price = Shop.PriceOf(Content, offer);
            var item = def.IsConsumable ? ItemInstance.Stack(def.Id, 1) : ItemInstance.Common(def.Id, offer.Level);
            var key = $"buy:{i}";
            var quantity = def.IsConsumable ? _quantities.GetValueOrDefault(key, 1) : 1;
            var usable = ItemText.CanUse(Content, item, classId);
            var detail = def.IsConsumable
                ? $"consumível · você tem {ItemText.CountOf(self.TryGet<Inventory>(), def.Id)}"
                : $"{(def.Slot is { } slot ? ItemText.SlotName(slot) : "?")} · nível {offer.Level}" + (usable ? "" : " · sua classe não usa");
            var row = Row(item, ItemText.Tooltip(Content, item, classId, null, isEquipped: true), def.Name, detail, !usable);

            var priceLabel = Small(ItemText.Gold(price), 12, 1f);
            priceLabel.AddThemeColorOverride("font_color", gold >= (long)price * quantity ? HudTheme.Gold : HudTheme.Warning);
            priceLabel.CustomMinimumSize = new Vector2(84, 0);
            priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(priceLabel);
            if (def.IsConsumable)
            {
                row.AddChild(QuantityBox(key, def.MaxStack, quantity));
            }

            var buy = SmallButton("Comprar");
            buy.Pressed += () => Send?.Invoke(new BuyIntent(self.Id, Npc!.Value, index, def.IsConsumable ? _quantities.GetValueOrDefault(key, 1) : 1));
            row.AddChild(buy);
            list.AddChild(row);
        }
    }

    private void SellRows(VBoxContainer list, EntitySnapshot self, string? classId)
    {
        var inv = self.TryGet<Inventory>();
        var any = false;
        for (var i = 0; inv is not null && i < inv.Capacity; i++)
        {
            if (inv[i] is not { } item || !Content.Items.TryGetValue(item.BaseId, out var def))
            {
                continue;
            }

            any = true;
            var slot = i;
            var price = Shop.SellPriceOf(Content, item);
            var key = $"sell:{i}:{item.BaseId}";
            var quantity = Math.Min(item.Count, _quantities.GetValueOrDefault(key, item.Count));
            var detail = def.IsConsumable
                ? $"consumível · {item.Count} na pilha"
                : $"{ItemText.RarityName(item.Rarity)} · {(def.Slot is { } s ? ItemText.SlotName(s) : "?")} · nível {item.Level}";
            var row = Row(item, ItemText.Tooltip(Content, item, classId, null, isEquipped: true, sellPrice: price), ItemText.Name(Content, item), detail,
                dim: false, nameColor: ItemText.RarityColor(item.Rarity));
            var priceLabel = Small(ItemText.Gold(price) + (item.Count > 1 ? " cada" : ""), 12, 1f);
            priceLabel.AddThemeColorOverride("font_color", HudTheme.Gold);
            priceLabel.CustomMinimumSize = new Vector2(96, 0);
            priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(priceLabel);
            if (item.Count > 1)
            {
                row.AddChild(QuantityBox(key, item.Count, quantity));
            }

            var precious = item.Rarity >= ItemRarity.Rare;
            var confirming = precious && _confirmSlot == slot;
            var sell = SmallButton(confirming ? "Confirmar?" : "Vender");
            if (confirming)
            {
                sell.AddThemeColorOverride("font_color", HudTheme.Warning);
                sell.TooltipText = $"{ItemText.RarityName(item.Rarity)}: clique de novo para vender.";
            }

            sell.Pressed += () =>
            {
                if (precious && _confirmSlot != slot)
                {
                    _confirmSlot = slot;
                    _confirmLeft = ConfirmSeconds;
                    Refresh();
                    return;
                }

                _confirmSlot = -1;
                Send?.Invoke(new SellIntent(self.Id, Npc!.Value, slot, Math.Min(item.Count, _quantities.GetValueOrDefault(key, item.Count))));
            };
            row.AddChild(sell);
            list.AddChild(row);
        }

        if (!any)
        {
            list.AddChild(Small("A mochila está vazia. Item equipado não se vende: desequipe antes (I).", 12, 0.7f, ListWidth - 20));
        }
    }

    /// <summary>Uma linha: ícone com tooltip, nome e detalhe; o preço e os botões entram depois.</summary>
    private HBoxContainer Row(ItemInstance item, string tooltip, string name, string detail, bool dim, Color? nameColor = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(new InventoryPanel.ItemSlotButton(Content, item, IconSize, empty: null, tooltip) { Unusable = dim });
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        var nameLabel = Small(name, 13, dim ? 0.55f : 1f);
        if (nameColor is { } c)
        {
            nameLabel.AddThemeColorOverride("font_color", c);
        }

        text.AddChild(nameLabel);
        text.AddChild(Small(detail, 10, dim ? 0.5f : 0.65f));
        row.AddChild(text);
        return row;
    }

    private static Button SmallButton(string text)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(78, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.AddThemeFontSizeOverride("font_size", 12);
        return button;
    }

    private SpinBox QuantityBox(string key, int max, int value)
    {
        var box = new SpinBox
        {
            MinValue = 1,
            MaxValue = Math.Max(1, Math.Min(max, Content.Rules.Shops.MaxQuantity)),
            Value = value,
            Rounded = true,
            TooltipText = "Quantidade",
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        box.GetLineEdit().AddThemeFontSizeOverride("font_size", 12);
        box.GetLineEdit().AddThemeConstantOverride("minimum_character_width", 2);
        // Só as setas: digitar no campo apertaria também as teclas 1 a 6 da barra.
        box.GetLineEdit().Editable = false;
        box.GetLineEdit().FocusMode = FocusModeEnum.None;
        box.ValueChanged += v =>
        {
            _quantities[key] = (int)v;
        };
        return box;
    }
}
