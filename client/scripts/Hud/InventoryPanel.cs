using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Janela de inventário e equipamento (T-222, tecla I; com o boneco na T-083). À esquerda, a figura da classe com os 6
/// espaços equipados em volta (<see cref="Equipment"/>); à direita, a grade do inventário (<see cref="Inventory"/>).
/// Arrastar da grade para o espaço certo equipa (o duplo clique também); arrastar do boneco para a grade desequipa;
/// arrastar da grade para fora da janela, ou o botão direito, larga no chão. Só manda intenções; quem decide é a
/// autoridade, e as recusas voltam como <see cref="ItemRejected"/>.
/// </summary>
public partial class InventoryPanel : ProgressionPanel
{
    private const int Columns = 6;
    private const float SlotSize = 42f;
    private const float DollWidth = 250f;
    private const float DollHeight = 290f;

    /// <summary>Espaço do boneco (T-089): borda de pedra de 9 px em volta do ícone de 32 px, em 1×.</summary>
    private const float EquipSlotSize = 50f;

    /// <summary>Zoom inteiro do nicho de pedra atrás do boneco.</summary>
    private const int NicheZoom = 2;

    /// <summary>Zoom inteiro da figura (sem borrão).</summary>
    private const int FigureZoom = 3;

    private const string DragInventory = "inventory";
    private const string DragEquipped = "equipped";

    private static SpriteCatalog? s_sprites;

    protected override bool StoneWindow => true;

    /// <summary>Última posição do mouse vista pelos eventos (para saber se o item foi solto fora da janela).</summary>
    private Vector2 _lastMouse;

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse)
        {
            _lastMouse = mouse.Position;
        }
    }

    /// <summary>O mouse está fora da janela do inventário?</summary>
    private bool PointerOutside() => !GetGlobalRect().HasPoint(_lastMouse);

    protected override string StateKey(EntitySnapshot self)
    {
        var inv = self.TryGet<Inventory>();
        var eq = self.TryGet<Equipment>();
        var s = self.TryGet<CharacterStats>();
        return string.Join("|",
            inv is null ? "-" : string.Join(";", inv.Slots.Select(Key)),
            eq is null ? "-" : string.Join(";", Enum.GetValues<EquipSlot>().Select(s => Key(eq[s]))),
            s?.WeaponDamage, s?.Armor, s?.Resistance, self.TryGet<Wallet>()?.Gold);
    }

    private static string Key(ItemInstance? item) =>
        item is null ? "" : $"{item.BaseId}:{item.Level}:{item.Rarity}:{item.Count}:{string.Join(",", item.Affixes)}";

    protected override void Build(VBoxContainer box, EntitySnapshot self)
    {
        box.AddChild(WindowTitle("Inventário"));
        var inventory = self.TryGet<Inventory>();
        var equipment = self.TryGet<Equipment>();
        var classId = self.TryGet<UnitInfo>()?.DefinitionId;
        if (inventory is null || equipment is null)
        {
            box.AddChild(Small("Este personagem não carrega itens.", 12));
            return;
        }

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 16);
        box.AddChild(columns);

        var left = new VBoxContainer();
        left.AddThemeConstantOverride("separation", 6);
        left.AddChild(Doll(self, equipment, classId));
        left.AddChild(Summary(self.TryGet<CharacterStats>()));
        columns.AddChild(left);

        var bag = new VBoxContainer();
        bag.AddThemeConstantOverride("separation", 4);
        var used = inventory.Slots.Count(i => i is not null);
        bag.AddChild(Small($"Mochila  {used} / {inventory.Capacity}", 13, 1f));
        var grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 3);
        grid.AddThemeConstantOverride("v_separation", 3);
        for (var i = 0; i < inventory.Capacity; i++)
        {
            var item = inventory[i];
            var index = i;
            ItemInstance? current = null;
            if (item is not null && Content.Items.TryGetValue(item.BaseId, out var def) && def.Slot is { } slot)
            {
                current = equipment[slot];
            }

            // T-323: consumível usa no duplo clique e vai para a barra como atalho do tipo (o mesmo dado da tela de habilidades).
            var consumable = item is not null && Content.Items.TryGetValue(item.BaseId, out var itemDef) && itemDef.IsConsumable;
            var drag = item is null ? null : new Godot.Collections.Dictionary { [DragInventory] = index };
            if (consumable)
            {
                drag![AbilitySlot.DragAbility] = item!.BaseId;
            }

            grid.AddChild(new ItemSlotButton(Content, item, SlotSize, empty: null,
                item is null ? "" : ItemText.Tooltip(Content, item, classId, current, isEquipped: false))
            {
                Unusable = item is not null && !ItemText.CanUse(Content, item, classId),
                DragData = drag,
                DoubleClick = item is null ? null
                    : consumable ? () => Send?.Invoke(new UseItemIntent(self.Id, index))
                    : () => Send?.Invoke(new EquipItemIntent(self.Id, index)),
                Right = item is null ? null : () => Send?.Invoke(new DropItemIntent(self.Id, index)),
                DroppedOutside = item is null ? null : () => Send?.Invoke(new DropItemIntent(self.Id, index)),
                Accepts = data => data.ContainsKey(DragEquipped),
                Dropped = data => Send?.Invoke(new UnequipItemIntent(self.Id, (EquipSlot)data[DragEquipped].AsInt32())),
                PointerOutside = PointerOutside,
            });
        }

        bag.AddChild(grid);
        bag.AddChild(GoldLine(self.TryGet<Wallet>()?.Gold ?? 0));
        bag.AddChild(Small("Arraste para o boneco ou duplo clique: equipar · poção: duplo clique usa, arraste para a barra · "
            + "do boneco para a mochila: desequipar · para fora da janela ou botão direito: largar no chão · F pega do chão",
            10, 0.6f, Columns * (SlotSize + 3)));
        columns.AddChild(bag);
    }

    /// <summary>Ouro do personagem (T-323), embaixo da grade: moeda desenhada e o valor em dourado.</summary>
    private static Control GoldLine(long gold)
    {
        var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", 6);
        line.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        line.AddChild(new GoldCoin { CustomMinimumSize = new Vector2(14, 14), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var label = Small(ItemText.Gold(gold), 14, 1f);
        label.AddThemeColorOverride("font_color", HudTheme.Gold);
        label.TooltipText = "Ouro: cai dos monstros e paga as lojas da Cidade (Templo e Ferreiro).";
        label.MouseFilter = MouseFilterEnum.Pass;
        line.AddChild(label);
        return line;
    }

    /// <summary>A figura da classe (parada, de frente) com os 6 espaços em volta: cabeça em cima, mãos e arma à esquerda, peito e acessório à direita (fora da figura, T-089), pés embaixo.</summary>
    private Control Doll(EntitySnapshot self, Equipment equipment, string? classId)
    {
        s_sprites ??= SpriteCatalog.Load();
        var doll = new DollFigure
        {
            CustomMinimumSize = new Vector2(DollWidth, DollHeight),
            Sprite = classId is null ? null : s_sprites.For(classId),
            Zoom = FigureZoom,
            Niche = HudTheme.DollNiche(),
            NicheZoom = NicheZoom,
        };
        var slotStyle = HudTheme.EquipSlot();
        var size = slotStyle is null ? SlotSize : EquipSlotSize;
        var center = DollWidth / 2 - size / 2;
        var positions = new Dictionary<EquipSlot, Vector2>
        {
            [EquipSlot.Head] = new(center, 4),
            [EquipSlot.Chest] = new(DollWidth - size - 6, 120),
            [EquipSlot.Hands] = new(6, 120),
            [EquipSlot.Weapon] = new(6, 176),
            [EquipSlot.Accessory] = new(DollWidth - size - 6, 176),
            [EquipSlot.Feet] = new(center, DollHeight - size - 4),
        };
        foreach (var (slot, position) in positions)
        {
            var item = equipment[slot];
            var target = slot;
            doll.AddChild(new ItemSlotButton(Content, item, size, empty: Silhouette(slot),
                item is null ? $"{ItemText.SlotName(slot)}: vazio" : ItemText.Tooltip(Content, item, classId, null, isEquipped: true))
            {
                Position = position,
                Frame = slotStyle,
                SlotLabel = ItemText.SlotName(slot),
                DragData = item is null ? null : new Godot.Collections.Dictionary { [DragEquipped] = (int)slot },
                DoubleClick = item is null ? null : () => Send?.Invoke(new UnequipItemIntent(self.Id, target)),
                Accepts = data => data.ContainsKey(DragInventory) && Fits(self, data[DragInventory].AsInt32(), target, classId),
                Dropped = data => Send?.Invoke(new EquipItemIntent(self.Id, data[DragInventory].AsInt32())),
                PointerOutside = PointerOutside,
            });
        }

        return doll;
    }

    /// <summary>O item da mochila vai neste espaço e a classe usa? (Só indicação: quem decide é o servidor.)</summary>
    private bool Fits(EntitySnapshot self, int index, EquipSlot slot, string? classId) =>
        self.TryGet<Inventory>() is { } inv && index >= 0 && index < inv.Capacity && inv[index] is { } item
        && Content.Items.TryGetValue(item.BaseId, out var def) && def.Slot == slot && ItemText.CanUse(Content, item, classId);

    /// <summary>Ícone de uma peça do tipo do espaço, bem apagado, para o espaço vazio.</summary>
    private Texture2D? Silhouette(EquipSlot slot) =>
        Content.Items.Values.Where(d => d.Slot == slot).OrderBy(d => d.Id, StringComparer.Ordinal).FirstOrDefault() is { } def
            ? IconCatalog.Shared.For(def.Id)
            : null;

    /// <summary>Resumo ao lado do boneco: dano da arma, armadura e resistência totais (do snapshot).</summary>
    private static Control Summary(CharacterStats? stats)
    {
        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 14);
        if (stats is null)
        {
            return line;
        }

        foreach (var (label, value) in new[]
        {
            ("Dano da arma", ProgressionText.Number(stats.WeaponDamage, "0.#")),
            ("Armadura", ProgressionText.Number(stats.Armor, "0")),
            ("Resistência", ProgressionText.Number(stats.Resistance, "0")),
        })
        {
            var pair = new VBoxContainer();
            pair.AddThemeConstantOverride("separation", 0);
            pair.AddChild(Small(label, 10, 0.65f));
            pair.AddChild(Small(value, 15, 1f));
            line.AddChild(pair);
        }

        return line;
    }

    /// <summary>Moeda de ouro desenhada (T-323): círculo dourado com borda escura e brilho.</summary>
    internal sealed partial class GoldCoin : Control
    {
        public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

        public override void _Draw()
        {
            var r = Mathf.Min(Size.X, Size.Y) / 2;
            var c = Size / 2;
            DrawCircle(c, r, new Color(0.35f, 0.24f, 0.05f));
            DrawCircle(c, r - 1.5f, HudTheme.Gold);
            DrawCircle(c, r - 4f, new Color(0.85f, 0.62f, 0.15f));
            DrawCircle(c + new Vector2(-r * 0.3f, -r * 0.3f), r * 0.22f, new Color(1f, 0.97f, 0.75f));
        }
    }

    /// <summary>Fundo do boneco: a figura da classe, parada e de frente, ampliada com zoom inteiro.</summary>
    private sealed partial class DollFigure : Control
    {
        public SpriteDefinition? Sprite { get; init; }

        public int Zoom { get; init; } = 3;

        /// <summary>Nicho de pedra atrás da figura (T-089); null = fundo liso de antes.</summary>
        public Texture2D? Niche { get; init; }

        public int NicheZoom { get; init; } = 2;

        public override void _Ready()
        {
            TextureFilter = TextureFilterEnum.Nearest;
            MouseFilter = MouseFilterEnum.Pass;
        }

        public override void _Draw()
        {
            if (Niche is not null)
            {
                var size = Niche.GetSize() * NicheZoom;
                DrawTextureRect(Niche, new Rect2(((Size - size) / 2).Floor(), size), tile: false);
            }
            else
            {
                DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.13f, 0.15f, 0.12f, 0.9f));
                DrawRect(new Rect2(Vector2.Zero, Size), new Color(1, 1, 1, 0.1f), filled: false);
            }
            if (Sprite?.For("idle") is not { } idle)
            {
                return;
            }

            var strip = idle.For(SpriteDirection.Down);
            var frame = strip.Frame(0);
            var feet = new Vector2(Size.X / 2, Size.Y - 58); // acima do espaço dos pés
            DrawTextureRectRegion(Sprite.Texture, new Rect2(feet - strip.Pivot * Zoom, frame.Size * Zoom), frame);
        }
    }

    /// <summary>
    /// Quadrado de item: borda na cor da raridade, ícone e tooltip em BBCode. Arrasta <see cref="DragData"/>; aceita o
    /// que <see cref="Accepts"/> aprova (borda dourada enquanto o item está em cima).
    /// </summary>
    internal sealed partial class ItemSlotButton : Button
    {
        private static readonly Color AcceptColor = new(1f, 0.85f, 0.45f);

        private readonly ContentCatalog _content;
        private readonly ItemInstance? _item;
        private readonly Texture2D? _empty;
        private readonly string _tooltip;
        private bool _dragging;
        private bool _dropHover;

        public ItemSlotButton(ContentCatalog content, ItemInstance? item, float size, Texture2D? empty, string tooltip)
        {
            _content = content;
            _item = item;
            _empty = empty;
            _tooltip = tooltip;
            CustomMinimumSize = new Vector2(size, size);
            Size = new Vector2(size, size);
            FocusMode = FocusModeEnum.None;
            TextureFilter = TextureFilterEnum.Nearest;
            TooltipText = tooltip.Length > 0 ? "item" : ""; // o texto de verdade vem de _MakeCustomTooltip
            Flat = true;
            GuiInput += OnGuiInput;
        }

        /// <summary>Moldura de pedra do espaço (T-089); o ícone vai na área de dentro, em 1×. Null = quadrado de antes.</summary>
        public StyleBox? Frame { get; init; }

        /// <summary>Item que a classe não usa: ícone esmaecido e um X.</summary>
        public bool Unusable { get; init; }

        /// <summary>Nome do espaço do boneco (vazio: aparece embaixo da silhueta).</summary>
        public string SlotLabel { get; init; } = "";

        public Godot.Collections.Dictionary? DragData { get; init; }

        public Action? DoubleClick { get; init; }

        public Action? Right { get; init; }

        /// <summary>Arrastado daqui e solto fora da janela.</summary>
        public Action? DroppedOutside { get; init; }

        public Func<Godot.Collections.Dictionary, bool>? Accepts { get; init; }

        public Action<Godot.Collections.Dictionary>? Dropped { get; init; }

        /// <summary>O mouse está fora da janela dona (soltar ali conta como "fora")?</summary>
        public Func<bool>? PointerOutside { get; init; }

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var label = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                ScrollActive = false,
                AutowrapMode = TextServer.AutowrapMode.Off,
                Text = _tooltip,
            };
            label.AddThemeFontSizeOverride("normal_font_size", 12);
            label.AddThemeFontSizeOverride("bold_font_size", 13);
            return label;
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (DragData is null || _item is null)
            {
                return default;
            }

            _dragging = true;
            SetDragPreview(AbilitySlot.DragPreview(IconCatalog.Shared.For(_item.BaseId), ItemText.Abbreviation(_content, _item)));
            return DragData;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            var ok = Accepts is not null && data.VariantType == Variant.Type.Dictionary && Accepts(data.AsGodotDictionary());
            SetDropHover(ok);
            return ok;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            SetDropHover(false);
            Dropped?.Invoke(data.AsGodotDictionary());
        }

        public override void _Notification(int what)
        {
            if (what == NotificationMouseExit || what == NotificationDragEnd)
            {
                SetDropHover(false);
            }

            if (what != NotificationDragEnd)
            {
                return;
            }

            if (_dragging && !GetViewport().GuiIsDragSuccessful() && PointerOutside?.Invoke() == true)
            {
                DroppedOutside?.Invoke();
            }

            _dragging = false;
        }

        private void SetDropHover(bool value)
        {
            if (_dropHover != value)
            {
                _dropHover = value;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            var font = ThemeDB.FallbackFont;
            if (Frame is not null)
            {
                DrawFramed(rect, font);
                return;
            }

            DrawRect(rect, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            if (_item is null)
            {
                if (_empty is not null)
                {
                    // Silhueta: o ícone de uma peça do tipo, quase apagado e sem cor.
                    DrawTextureRect(_empty, rect.Grow(-5), tile: false, new Color(0.8f, 0.85f, 0.9f, 0.16f));
                }

                DrawRect(rect, _dropHover ? AcceptColor : new Color(1, 1, 1, 0.15f), filled: false, width: _dropHover ? 2.5f : 1f);
                return;
            }

            var color = ItemText.RarityColor(_item.Rarity);
            DrawRect(rect, new Color(color, 0.18f));
            DrawRect(rect, _dropHover ? AcceptColor : color, filled: false, width: _dropHover || IsHovered() ? 3f : 2f);
            if (IconCatalog.Shared.For(_item.BaseId) is { } icon)
            {
                DrawTextureRect(icon, rect.Grow(-3), tile: false, Unusable ? new Color(1, 1, 1, 0.35f) : Colors.White);
            }
            else
            {
                var text = ItemText.Abbreviation(_content, _item);
                DrawString(font, new Vector2(0, Size.Y / 2 + 5), text, HorizontalAlignment.Center, Size.X, 14,
                    Unusable ? new Color(1, 1, 1, 0.35f) : Colors.White);
            }

            if (_content.Items.TryGetValue(_item.BaseId, out var def) && def.IsConsumable)
            {
                // T-323: pilha com a quantidade no canto (o nível não diz nada de um consumível).
                DrawCount(this, font, rect, _item.Count);
            }
            else
            {
                DrawString(font, new Vector2(3, Size.Y - 4), _item.Level.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    HorizontalAlignment.Left, -1, 9, new Color(1, 1, 1, 0.6f));
            }
            if (Unusable)
            {
                DrawLine(new Vector2(6, 6), Size - new Vector2(6, 6), new Color(1f, 0.4f, 0.35f, 0.8f), 2f);
            }
        }

        /// <summary>Quantidade (só de 2 para cima) no canto de baixo à direita, com contorno para ler sobre o ícone.</summary>
        public static void DrawCount(CanvasItem canvas, Font font, Rect2 rect, int count)
        {
            if (count <= 1)
            {
                return;
            }

            var text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var at = new Vector2(rect.Position.X, rect.End.Y - 3);
            canvas.DrawStringOutline(font, at, text, HorizontalAlignment.Right, rect.Size.X - 3, 12, 4, Colors.Black);
            canvas.DrawString(font, at, text, HorizontalAlignment.Right, rect.Size.X - 3, 12, Colors.White);
        }

        /// <summary>Espaço de pedra: a moldura em volta, a raridade na borda de dentro, o ícone em 1× no meio.</summary>
        private void DrawFramed(Rect2 rect, Font font)
        {
            Frame!.Draw(GetCanvasItem(), rect);
            var inner = rect.Grow(-Frame.GetMargin(Side.Left));
            if (_item is null)
            {
                if (_empty is not null)
                {
                    DrawTextureRect(_empty, inner, tile: false, new Color(0.8f, 0.85f, 0.9f, 0.16f));
                }

                if (_dropHover)
                {
                    DrawRect(inner.Grow(1), AcceptColor, filled: false, width: 2f);
                }

                return;
            }

            var color = ItemText.RarityColor(_item.Rarity);
            DrawRect(inner, new Color(color, 0.18f));
            if (IconCatalog.Shared.For(_item.BaseId) is { } icon)
            {
                DrawTextureRect(icon, inner, tile: false, Unusable ? new Color(1, 1, 1, 0.35f) : Colors.White);
            }

            DrawRect(inner.Grow(1), _dropHover ? AcceptColor : color, filled: false, width: _dropHover || IsHovered() ? 2f : 1f);
            DrawString(font, inner.Position + new Vector2(1, inner.Size.Y - 1),
                _item.Level.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1, 9, new Color(1, 1, 1, 0.7f));
            if (Unusable)
            {
                DrawLine(inner.Position + new Vector2(3, 3), inner.End - new Vector2(3, 3), new Color(1f, 0.4f, 0.35f, 0.8f), 2f);
            }
        }

        private void OnGuiInput(InputEvent e)
        {
            switch (e)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } when Right is not null:
                    Right();
                    AcceptEvent();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true } when DoubleClick is not null:
                    DoubleClick();
                    AcceptEvent();
                    break;
            }
        }
    }
}
