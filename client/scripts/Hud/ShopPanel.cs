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
/// inventário, com o preço de venda no tooltip e na linha; item raro ou épico pede um segundo clique. A quantidade (T-384)
/// tem − e +, "Máx." e campo digitável (Enter confirma, Esc sai do campo); o preço mostra o total. Só manda intenções:
/// quem decide é a autoridade, e as recusas voltam como <see cref="ShopRejected"/>. O <c>Hud</c> fecha a janela quando o
/// personagem se afasta do NPC.
/// </summary>
public partial class ShopPanel : ProgressionPanel
{
    private const float ListWidth = 580f;
    private const float ListHeight = 270f;
    private const float IconSize = 40f;
    private const float RowHeight = 46f;
    private const double ConfirmSeconds = 3.0;

    private readonly Dictionary<string, int> _quantities = new(StringComparer.Ordinal);
    private bool _selling;
    private int _confirmSlot = -1;
    private double _confirmLeft;
    private float _scroll;
    private ulong _focusLostFrame = ulong.MaxValue;

    /// <summary>
    /// T-384: o campo de quantidade está com o foco; as teclas são números, não comandos. Vale também no frame em que o
    /// campo perde o foco, para o Esc ou o Enter que tirou o foco não fechar a loja nem limpar o alvo.
    /// </summary>
    public bool IsTyping => (Visible && GetViewport()?.GuiGetFocusOwner() is LineEdit edit && IsAncestorOf(edit))
        || Engine.GetProcessFrames() == _focusLostFrame;

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

    /// <summary>T-384: clicar fora do campo de quantidade tira o foco dele (o clique não vira ataque nesse frame).</summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true } click && GetViewport()?.GuiGetFocusOwner() is LineEdit edit
            && IsAncestorOf(edit) && !edit.GetGlobalRect().HasPoint(click.Position))
        {
            edit.ReleaseFocus();
        }
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
            var inv = self.TryGet<Inventory>();
            var shopLimit = Content.Rules.Shops.MaxQuantity;
            var room = ShopQuantity.Room(inv, item, def.MaxStack);
            var limit = def.IsConsumable ? ShopQuantity.BuyLimit(shopLimit, room) : 1;
            var quantity = ShopQuantity.Clamp(_quantities.GetValueOrDefault(key, 1), limit);
            var usable = ItemText.CanUse(Content, item, classId);
            var detail = def.IsConsumable
                ? $"consumível · você tem {ItemText.CountOf(inv, def.Id)}"
                : $"{(def.Slot is { } slot ? ItemText.SlotName(slot) : "?")} · nível {offer.Level}" + (usable ? "" : " · sua classe não usa");
            var row = Row(item, ItemText.Tooltip(Content, item, classId, null, isEquipped: true), def.Name, detail, !usable);

            var showPrice = PriceColumn(row, price, quantity, q => gold >= ShopQuantity.Total(price, q) ? HudTheme.Gold : HudTheme.Warning);
            void BuyNow() => Send?.Invoke(new BuyIntent(self.Id, Npc!.Value, index, def.IsConsumable ? CurrentQuantity(key, limit, 1) : 1));
            if (def.IsConsumable)
            {
                row.AddChild(QuantityControl(key, limit, quantity, () => ShopQuantity.BuyMax(price, gold, room, shopLimit), showPrice, BuyNow,
                    "Máximo que cabe no seu ouro, na mochila e no limite da loja"));
            }

            var buy = SmallButton("Comprar");
            buy.Pressed += BuyNow;
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
            var quantity = ShopQuantity.Clamp(_quantities.GetValueOrDefault(key, item.Count), item.Count);
            var detail = def.IsConsumable
                ? $"consumível · {item.Count} na pilha"
                : $"{ItemText.RarityName(item.Rarity)} · {(def.Slot is { } s ? ItemText.SlotName(s) : "?")} · nível {item.Level}";
            var row = Row(item, ItemText.Tooltip(Content, item, classId, null, isEquipped: true, sellPrice: price), ItemText.Name(Content, item), detail,
                dim: false, nameColor: ItemText.RarityColor(item.Rarity));
            var showPrice = PriceColumn(row, price, quantity, _ => HudTheme.Gold);
            var precious = item.Rarity >= ItemRarity.Rare;
            var confirming = precious && _confirmSlot == slot;
            var sell = SmallButton(confirming ? "Confirmar?" : "Vender");
            if (confirming)
            {
                sell.AddThemeColorOverride("font_color", HudTheme.Warning);
                sell.TooltipText = $"{ItemText.RarityName(item.Rarity)}: clique de novo para vender.";
            }

            void SellNow()
            {
                if (precious && _confirmSlot != slot)
                {
                    _confirmSlot = slot;
                    _confirmLeft = ConfirmSeconds;
                    Refresh();
                    return;
                }

                _confirmSlot = -1;
                Send?.Invoke(new SellIntent(self.Id, Npc!.Value, slot, CurrentQuantity(key, item.Count, item.Count)));
            }

            if (item.Count > 1)
            {
                row.AddChild(QuantityControl(key, item.Count, quantity, () => item.Count, showPrice, SellNow, "A pilha inteira"));
            }

            sell.Pressed += SellNow;
            row.AddChild(sell);
            list.AddChild(row);
        }

        if (!any)
        {
            list.AddChild(Small($"A mochila está vazia. Item equipado não se vende: desequipe antes{Hud.KeyHint(Input.InputActions.InventoryPanel)}.", 12, 0.7f, ListWidth - 20));
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

    /// <summary>Quantidade escolhida para a linha, entre 1 e <paramref name="limit"/>.</summary>
    private int CurrentQuantity(string key, int limit, int fallback) => ShopQuantity.Clamp(_quantities.GetValueOrDefault(key, fallback), limit);

    /// <summary>
    /// Coluna do preço (T-384): o total da quantidade escolhida e, embaixo, o preço de uma unidade quando a quantidade passa
    /// de 1. Devolve a função que redesenha a coluna quando a quantidade muda (sem refazer a lista).
    /// </summary>
    private static Action<int> PriceColumn(HBoxContainer row, int price, int quantity, Func<int, Color> color)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(92, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        column.AddThemeConstantOverride("separation", -2);
        var total = Small("", 13, 1f);
        total.HorizontalAlignment = HorizontalAlignment.Right;
        var each = Small(ItemText.Gold(price) + " cada", 10, 0.6f);
        each.HorizontalAlignment = HorizontalAlignment.Right;
        column.AddChild(total);
        column.AddChild(each);
        row.AddChild(column);

        void Show(int q)
        {
            total.Text = ItemText.Gold(ShopQuantity.Total(price, q));
            total.AddThemeColorOverride("font_color", color(q));
            each.Visible = q > 1;
        }

        Show(quantity);
        return Show;
    }

    /// <summary>
    /// Quantidade (T-384): − e +, o campo e "Máx.". O campo aceita digitar (só números, até <paramref name="limit"/>);
    /// com ele em foco o <c>Hud</c> desliga as teclas do jogo (<see cref="IsTyping"/>). Enter confirma
    /// (<paramref name="submit"/>), Esc só tira o foco.
    /// </summary>
    private HBoxContainer QuantityControl(string key, int limit, int value, Func<int> max, Action<int> changed, Action submit, string maxTooltip)
    {
        var box = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        box.AddThemeConstantOverride("separation", 2);
        var minus = StepButton("−", "Menos um");
        var field = new LineEdit
        {
            Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Alignment = HorizontalAlignment.Center,
            MaxLength = 3,
            CustomMinimumSize = new Vector2(40, 26),
            SelectAllOnFocus = true,
            ContextMenuEnabled = false,
            TooltipText = $"Quantidade (até {limit}): digite e aperte Enter; Esc sai do campo",
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        field.AddThemeFontSizeOverride("font_size", 13);
        var plus = StepButton("+", "Mais um");
        var maxButton = new Button { Text = "Máx.", FocusMode = FocusModeEnum.None, TooltipText = maxTooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        maxButton.AddThemeFontSizeOverride("font_size", 11);

        var current = value;
        void Set(int quantity, bool writeText = true)
        {
            current = ShopQuantity.Clamp(quantity, limit);
            _quantities[key] = current;
            var text = current.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (writeText && field.Text != text)
            {
                field.Text = text;
                field.CaretColumn = text.Length;
            }

            minus.Disabled = current <= 1;
            plus.Disabled = current >= limit;
            changed(current);
        }

        minus.Pressed += () => Set(current - 1);
        plus.Pressed += () => Set(current + 1);
        maxButton.Pressed += () => Set(max());
        field.TextChanged += text =>
        {
            // Vazio: o jogador ainda está digitando; o número anterior continua valendo até sair do campo.
            if (ShopQuantity.Parse(text, limit) is { } typed)
            {
                Set(typed, writeText: typed.ToString(System.Globalization.CultureInfo.InvariantCulture) != text);
            }
        };
        field.TextSubmitted += _ =>
        {
            field.ReleaseFocus();
            submit();
        };
        field.GuiInput += e =>
        {
            if (e.IsActionPressed("ui_cancel"))
            {
                field.ReleaseFocus();
                field.AcceptEvent();
            }
        };
        field.FocusExited += () =>
        {
            _focusLostFrame = Engine.GetProcessFrames();
            Set(current);
        };

        Set(current);
        box.AddChild(minus);
        box.AddChild(field);
        box.AddChild(plus);
        box.AddChild(maxButton);
        return box;
    }

    private static Button StepButton(string text, string tooltip)
    {
        var button = new Button { Text = text, TooltipText = tooltip, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(26, 26), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.AddThemeFontSizeOverride("font_size", 15);
        return button;
    }
}
