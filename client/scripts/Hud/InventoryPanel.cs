using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Janela de inventário e equipamento (T-222, tecla I). Mostra os 6 espaços equipados e a grade do inventário
/// (<see cref="Equipment"/>, <see cref="Inventory"/> do snapshot). Clique no inventário equipa, clique no equipado
/// desequipa, botão direito larga no chão. Só manda intenções; recusas voltam como <see cref="ItemRejected"/>.
/// </summary>
public partial class InventoryPanel : ProgressionPanel
{
    private const int Columns = 6;
    private const float SlotSize = 40f;

    protected override string StateKey(EntitySnapshot self)
    {
        var inv = self.TryGet<Inventory>();
        var eq = self.TryGet<Equipment>();
        return string.Join("|",
            inv is null ? "-" : string.Join(";", inv.Slots.Select(Key)),
            eq is null ? "-" : string.Join(";", Enum.GetValues<EquipSlot>().Select(s => Key(eq[s]))));
    }

    private static string Key(ItemInstance? item) =>
        item is null ? "" : $"{item.BaseId}:{item.Level}:{item.Rarity}:{string.Join(",", item.Affixes)}";

    protected override void Build(VBoxContainer box, EntitySnapshot self)
    {
        box.AddChild(Title("Inventário"));
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

        var equipped = new VBoxContainer();
        equipped.AddThemeConstantOverride("separation", 4);
        equipped.AddChild(Small("Equipado", 12, 1f));
        foreach (var slot in Enum.GetValues<EquipSlot>())
        {
            // O nome do item fica no tooltip: a janela cabe ao lado da tela de personagem.
            var item = equipment[slot];
            var target = slot;
            equipped.AddChild(new ItemSlotButton(Content, item, SlotSize, ItemText.SlotName(slot)[..3],
                item is null ? $"{ItemText.SlotName(slot)}: vazio" : ItemText.Tooltip(Content, item, classId, null, isEquipped: true),
                left: item is null ? null : () => Send?.Invoke(new UnequipItemIntent(self.Id, target)),
                right: null));
        }

        columns.AddChild(equipped);

        var bag = new VBoxContainer();
        bag.AddThemeConstantOverride("separation", 4);
        var used = inventory.Slots.Count(i => i is not null);
        bag.AddChild(Small($"Inventário  {used} / {inventory.Capacity}", 13, 1f));
        var grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 3);
        grid.AddThemeConstantOverride("v_separation", 3);
        for (var i = 0; i < inventory.Capacity; i++)
        {
            var item = inventory[i];
            var index = i;
            ItemInstance? current = null;
            if (item is not null && Content.Items.TryGetValue(item.BaseId, out var def))
            {
                current = equipment[def.Slot];
            }

            grid.AddChild(new ItemSlotButton(Content, item, SlotSize, "",
                item is null ? "" : ItemText.Tooltip(Content, item, classId, current, isEquipped: false),
                left: item is null ? null : () => Send?.Invoke(new EquipItemIntent(self.Id, index)),
                right: item is null ? null : () => Send?.Invoke(new DropItemIntent(self.Id, index)))
            {
                Unusable = item is not null && !ItemText.CanUse(Content, item, classId),
            });
        }

        bag.AddChild(grid);
        bag.AddChild(Small("Clique equipa · botão direito larga · F pega o item do chão", 10, 0.6f, 250));
        columns.AddChild(bag);
    }

    /// <summary>Quadrado de item: borda na cor da raridade, sigla do item e tooltip em BBCode.</summary>
    private sealed partial class ItemSlotButton : Button
    {
        private readonly ContentCatalog _content;
        private readonly ItemInstance? _item;
        private readonly string _empty;
        private readonly string _tooltip;
        private readonly Action? _right;

        public ItemSlotButton(ContentCatalog content, ItemInstance? item, float size, string empty, string tooltip, Action? left, Action? right)
        {
            _content = content;
            _item = item;
            _empty = empty;
            _tooltip = tooltip;
            _right = right;
            CustomMinimumSize = new Vector2(size, size);
            FocusMode = FocusModeEnum.None;
            TooltipText = tooltip.Length > 0 ? "item" : ""; // o texto de verdade vem de _MakeCustomTooltip
            Flat = true;
            if (left is not null)
            {
                Pressed += left;
            }

            GuiInput += OnGuiInput;
        }

        /// <summary>Item que a classe não usa: sigla esmaecida e um X.</summary>
        public bool Unusable { get; init; }

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

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            DrawRect(rect, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            var font = ThemeDB.FallbackFont;
            if (_item is null)
            {
                DrawRect(rect, new Color(1, 1, 1, 0.15f), filled: false, width: 1f);
                if (_empty.Length > 0)
                {
                    DrawString(font, new Vector2(0, Size.Y / 2 + 4), _empty, HorizontalAlignment.Center, Size.X, 10, new Color(1, 1, 1, 0.3f));
                }

                return;
            }

            var color = ItemText.RarityColor(_item.Rarity);
            DrawRect(rect, new Color(color, 0.18f));
            DrawRect(rect, color, filled: false, width: IsHovered() ? 3f : 2f);
            var text = ItemText.Abbreviation(_content, _item);
            DrawString(font, new Vector2(0, Size.Y / 2 + 5), text, HorizontalAlignment.Center, Size.X, 14,
                Unusable ? new Color(1, 1, 1, 0.35f) : Colors.White);
            DrawString(font, new Vector2(3, Size.Y - 4), _item.Level.ToString(System.Globalization.CultureInfo.InvariantCulture),
                HorizontalAlignment.Left, -1, 9, new Color(1, 1, 1, 0.6f));
            if (Unusable)
            {
                DrawLine(new Vector2(6, 6), Size - new Vector2(6, 6), new Color(1f, 0.4f, 0.35f, 0.8f), 2f);
            }
        }

        private void OnGuiInput(InputEvent e)
        {
            if (_right is not null && e is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
            {
                _right();
                AcceptEvent();
            }
        }
    }
}
