using Game.Core.CombatLog;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Cores e nomes exibidos no HUD (somente apresentação).</summary>
public static class HudTheme
{
    public static readonly Color Health = new(0.25f, 0.75f, 0.30f);
    public static readonly Color HealthLow = new(0.85f, 0.25f, 0.20f);
    public static readonly Color Shield = new(0.85f, 0.85f, 0.95f, 0.75f);
    public static readonly Color Cast = new(0.95f, 0.75f, 0.25f);
    public static readonly Color CastEnemy = new(0.90f, 0.35f, 0.25f);
    public static readonly Color Panel = new(0.06f, 0.06f, 0.08f, 0.82f);
    public static readonly Color BarBack = new(0.12f, 0.12f, 0.14f, 0.95f);
    public static readonly Color Removable = new(0.55f, 0.95f, 0.55f);
    public static readonly Color Xp = new(0.70f, 0.45f, 0.95f);
    public static readonly Color Suggested = new(1f, 0.85f, 0.45f);
    public static readonly Color Warning = new(1f, 0.55f, 0.4f);

    /// <summary>Ouro (T-323): valor no inventário, na loja e o "+N" sobre o personagem.</summary>
    public static readonly Color Gold = new(1f, 0.82f, 0.30f);

    /// <summary>Borda dourada das barras nos quadros de pedra (kit pedra e ouro, T-086).</summary>
    public static readonly Color BarBorderGold = new(0.80f, 0.64f, 0.26f);

    private const string StonePanelPath = "res://assets/art/ui/panel.png";
    private const int StonePanelMargin = 22;
    private static StyleBoxTexture? _stonePanel;

    private const string UiDirectory = "res://assets/art/ui/";
    private static readonly Dictionary<string, StyleBox?> UiStyles = new(StringComparer.Ordinal);

    /// <summary>Texto claro dos painéis de pedra.</summary>
    public static readonly Color StoneText = new(0.94f, 0.91f, 0.82f);

    /// <summary>
    /// 9-slice de uma peça do kit pedra e ouro (<c>assets/art/ui/&lt;file&gt;</c>), desenhada em 1×; null sem o arquivo.
    /// </summary>
    public static StyleBox? UiPiece(string file, int margin, float contentX, float contentY)
    {
        var key = $"{file}|{margin}|{contentX}|{contentY}";
        if (UiStyles.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var path = UiDirectory + file;
        var texture = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        StyleBox? style = texture is null ? null : new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = margin,
            TextureMarginRight = margin,
            TextureMarginTop = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = contentX,
            ContentMarginRight = contentX,
            ContentMarginTop = contentY,
            ContentMarginBottom = contentY,
        };
        UiStyles[key] = style;
        return style;
    }

    /// <summary>
    /// Moldura de janela de pedra e ouro (T-089): 9-slice de <c>window_frame.png</c> em 1×, com pouca margem em cima para a
    /// placa do título (<see cref="WindowTitlePlate"/>) ficar por cima da borda. Embaixo, a margem passa da borda de 18 px
    /// (pedra e filete dourado) para o conteúdo não encostar no filete. Null sem o arquivo.
    /// </summary>
    public static StyleBox? WindowFrame()
    {
        if (UiPiece("window_frame.png", 18, 24, 18) is not StyleBoxTexture piece)
        {
            return null;
        }

        var style = (StyleBoxTexture)piece.Duplicate();
        style.ContentMarginTop = 3;
        style.ContentMarginBottom = 26;
        return style;
    }

    /// <summary>Placa de pedra do título da janela (T-089); null sem o arquivo.</summary>
    public static StyleBox? WindowTitlePlate() => UiPiece("window_title.png", 6, 18, 2);

    /// <summary>Espaço de equipamento de pedra (T-089): 9-slice de <c>equip_slot.png</c>, borda de 9 px; null sem o arquivo.</summary>
    public static StyleBox? EquipSlot() => UiPiece("equip_slot.png", 9, 9, 9);

    /// <summary>Nicho de pedra atrás do boneco (T-089); null sem o arquivo.</summary>
    public static Texture2D? DollNiche() =>
        ResourceLoader.Exists(UiDirectory + "doll_niche.png") ? ResourceLoader.Load<Texture2D>(UiDirectory + "doll_niche.png") : null;

    /// <summary>Textura de uma peça do kit (<c>assets/art/ui/&lt;file&gt;</c>), desenhada em 1×; null sem o arquivo.</summary>
    public static Texture2D? UiTexture(string file) =>
        ResourceLoader.Exists(UiDirectory + file) ? ResourceLoader.Load<Texture2D>(UiDirectory + file) : null;

    /// <summary>Célula de pedra de uma habilidade (T-097): 9-slice de <c>skill_cell.png</c>; null sem o arquivo.</summary>
    public static StyleBox? SkillCell() => UiPiece("skill_cell.png", 10, 8, 8);

    /// <summary>
    /// Botão redondo de pedra com o + dourado (T-097), no lugar do "+" de texto; sem a peça, o botão fica como antes.
    /// </summary>
    public static void StonePlusButton(Button button)
    {
        if (UiTexture("skill_plus.png") is not { } texture)
        {
            return;
        }

        StyleBoxTexture Style(Color modulate) => new() { Texture = texture, ModulateColor = modulate };
        button.Text = "";
        button.AddThemeStyleboxOverride("normal", Style(Colors.White));
        button.AddThemeStyleboxOverride("hover", Style(new Color(1.2f, 1.2f, 1.1f)));
        button.AddThemeStyleboxOverride("pressed", Style(new Color(0.8f, 0.8f, 0.8f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("disabled", Style(new Color(1, 1, 1, 0.35f)));
        button.CustomMinimumSize = texture.GetSize();
        button.Size = texture.GetSize();
    }

    /// <summary>Moldura fina de pedra (quadro de membro do grupo, T-311).</summary>
    public static StyleBox MemberFrame() =>
        UiPiece("frame_member.png", 9, 10, 7)
        ?? new StyleBoxFlat { BgColor = Panel, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4 };

    /// <summary>
    /// Botão de pedra e ouro nos quatro estados (T-311); sem as peças, o botão fica com o tema padrão. Compacto: as mesmas
    /// peças com menos folga em volta do texto (os botões fixos do HUD, como os do grupo).
    /// </summary>
    public static void StoneButton(Button button, bool compact = false)
    {
        foreach (var (state, file) in new[] { ("normal", "button_normal.png"), ("hover", "button_hover.png"),
                     ("pressed", "button_pressed.png"), ("disabled", "button_disabled.png"), ("focus", "button_hover.png") })
        {
            if ((compact ? UiPiece(file, 9, 9, 2) : UiPiece(file, 9, 14, 6)) is { } style)
            {
                button.AddThemeStyleboxOverride(state, style);
            }
        }

        button.AddThemeColorOverride("font_color", StoneText);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", StoneText);
        button.AddThemeColorOverride("font_outline_color", Colors.Black);
        button.AddThemeConstantOverride("outline_size", 4);
    }

    /// <summary>Coroa do líder do grupo (T-311); null sem o arquivo.</summary>
    public static Texture2D? Crown() =>
        ResourceLoader.Exists(UiDirectory + "crown.png") ? ResourceLoader.Load<Texture2D>(UiDirectory + "crown.png") : null;

    /// <summary>
    /// Espaço de pedra da mochila (T-099): 9-slice de <c>bag_slot.png</c>, desenhado em 1× no tamanho do espaço; o ícone
    /// de 32 px vai no meio. Null sem o arquivo (volta ao quadrado de antes).
    /// </summary>
    public static StyleBox? BagSlot() => UiPiece("bag_slot.png", 10, 6, 6);

    /// <summary>Plaquinha de pedra atrás da quantidade de um item empilhável (T-099); null sem o arquivo.</summary>
    public static StyleBox? BagCountTag() => UiPiece("bag_count.png", 4, 3, 0);

    /// <summary>Placa de pedra do mostrador de ouro, embaixo da mochila (T-099); null sem o arquivo.</summary>
    public static StyleBox? BagGoldPlate()
    {
        if (UiPiece("bag_gold.png", 14, 16, 2) is not StyleBoxTexture piece)
        {
            return null;
        }

        var style = (StyleBoxTexture)piece.Duplicate();
        style.TextureMarginTop = 8;
        style.TextureMarginBottom = 8;
        style.ContentMarginBottom = 3;
        return style;
    }

    /// <summary>Moeda de ouro desenhada em 1× (T-099), ao lado do valor; null sem o arquivo.</summary>
    public static Texture2D? BagCoin() => UiTexture("bag_coin.png");

    /// <summary>
    /// Painel de pedra com cantos dourados (kit pedra e ouro, T-086): 9-slice de <c>assets/art/ui/panel.png</c>,
    /// desenhado em 1×. Sem a textura (export sem o arquivo), devolve o painel escuro de sempre.
    /// </summary>
    public static StyleBox StonePanel()
    {
        if (_stonePanel is not null)
        {
            return _stonePanel;
        }

        var texture = ResourceLoader.Exists(StonePanelPath) ? ResourceLoader.Load<Texture2D>(StonePanelPath) : null;
        if (texture is null)
        {
            return new StyleBoxFlat { BgColor = Panel, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4 };
        }

        _stonePanel = new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = StonePanelMargin,
            TextureMarginRight = StonePanelMargin,
            TextureMarginTop = StonePanelMargin,
            TextureMarginBottom = StonePanelMargin,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 21,
            ContentMarginBottom = 24,
        };
        return _stonePanel;
    }

    private static StyleBox? _stonePanelCompact;

    /// <summary>
    /// O mesmo painel de pedra em meia escala (bordas, rachaduras e cantos dourados com metade da espessura), para os
    /// quadros fixos do HUD (jogador, chefe) ocuparem menos tela sem perder o estilo. Gerado do <c>panel.png</c> na
    /// primeira chamada, com filtro Lanczos (o vizinho mais próximo apagava os cantos de ouro).
    /// </summary>
    public static StyleBox StonePanelCompact()
    {
        if (_stonePanelCompact is not null)
        {
            return _stonePanelCompact;
        }

        if (StonePanel() is not StyleBoxTexture { Texture: { } full } || full.GetImage() is not { } image)
        {
            return _stonePanelCompact = StonePanel();
        }

        image = (Image)image.Duplicate();
        if (image.IsCompressed())
        {
            image.Decompress();
        }

        image.Resize(image.GetWidth() / 2, image.GetHeight() / 2, Image.Interpolation.Lanczos);
        const int margin = StonePanelMargin / 2;
        return _stonePanelCompact = new StyleBoxTexture
        {
            Texture = ImageTexture.CreateFromImage(image),
            TextureMarginLeft = margin,
            TextureMarginRight = margin,
            TextureMarginTop = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 12,
        };
    }

    /// <summary>
    /// Linha de pedra de um atributo na tela de personagem (T-098): 9-slice de <c>attr_row.png</c> em 1×; null sem o
    /// arquivo (a linha volta ao fundo escuro de antes).
    /// </summary>
    /// <remarks>A borda esquerda é a metade da divisória que encosta no encaixe (<see cref="AttributeSocket"/>).</remarks>
    public static StyleBox? AttributeRow() => UiPiece9("attr_row.png", (4, 7, 7, 8), (11, 5, 11, 5));

    /// <summary>
    /// Encaixe de pedra do ícone, colado à esquerda da linha do atributo (T-098): 9-slice de <c>attr_socket.png</c>, que
    /// estica só o fundo escuro, com o ícone de 34 px no meio; null sem o arquivo (o ícone fica dentro da linha).
    /// </summary>
    public static StyleBox? AttributeSocket() => UiPiece9("attr_socket.png", (9, 9, 5, 9), (7, 6, 3, 6));

    /// <summary>
    /// Botão de pedra de somar ponto (+1 e +5 da tela de personagem, T-098): 9-slice de <c>attr_add.png</c>, com o texto do
    /// Godot por cima; mais claro no hover, mais escuro no clique. Sem a peça, o botão fica como antes.
    /// </summary>
    public static void StoneAddButton(Button button)
    {
        if (UiPiece9("attr_add.png", (8, 8, 8, 8), (7, 3, 7, 3)) is not StyleBoxTexture piece)
        {
            return;
        }

        StyleBoxTexture Style(Color modulate)
        {
            var style = (StyleBoxTexture)piece.Duplicate();
            style.ModulateColor = modulate;
            return style;
        }

        button.AddThemeStyleboxOverride("normal", piece);
        button.AddThemeStyleboxOverride("hover", Style(new Color(1.25f, 1.25f, 1.15f)));
        button.AddThemeStyleboxOverride("pressed", Style(new Color(0.8f, 0.8f, 0.8f)));
        button.AddThemeStyleboxOverride("disabled", Style(new Color(1, 1, 1, 0.35f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", Suggested);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Suggested);
        button.AddThemeColorOverride("font_outline_color", Colors.Black);
        button.AddThemeConstantOverride("outline_size", 4);
    }

    /// <summary>
    /// 9-slice de uma peça do kit com bordas diferentes em cada lado (esquerda, cima, direita, baixo), desenhada em 1×;
    /// null sem o arquivo (T-098).
    /// </summary>
    private static StyleBox? UiPiece9(string file, (int L, int T, int R, int B) margin, (float L, float T, float R, float B) content)
    {
        var key = $"{file}|{margin}|{content}";
        if (UiStyles.TryGetValue(key, out var cached))
        {
            return cached;
        }

        StyleBox? style = UiTexture(file) is not { } texture ? null : new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = margin.L,
            TextureMarginTop = margin.T,
            TextureMarginRight = margin.R,
            TextureMarginBottom = margin.B,
            ContentMarginLeft = content.L,
            ContentMarginTop = content.T,
            ContentMarginRight = content.R,
            ContentMarginBottom = content.B,
        };
        UiStyles[key] = style;
        return style;
    }

    public static Color Resource(string type) => Key(type) switch
    {
        "vigor" => new Color(0.90f, 0.45f, 0.20f),
        "focus" => new Color(0.85f, 0.85f, 0.30f),
        "mana" => new Color(0.30f, 0.50f, 0.95f),
        "faith" => new Color(0.95f, 0.85f, 0.55f),
        _ => new Color(0.6f, 0.6f, 0.6f),
    };

    public static string ResourceName(string type) => Key(type) switch
    {
        "vigor" => "Vigor",
        "focus" => "Foco",
        "mana" => "Mana",
        "faith" => "Fé",
        var other => other,
    };

    public static Color School(DamageSchool school) => school switch
    {
        DamageSchool.Physical => new Color(1.00f, 0.95f, 0.90f),
        DamageSchool.Arcane => new Color(0.80f, 0.55f, 1.00f),
        DamageSchool.Fire => new Color(1.00f, 0.55f, 0.20f),
        DamageSchool.Frost => new Color(0.55f, 0.85f, 1.00f),
        DamageSchool.Holy => new Color(1.00f, 0.90f, 0.45f),
        DamageSchool.Nature => new Color(0.55f, 0.90f, 0.35f),
        _ => Colors.White,
    };

    public static Color Heal => new(0.40f, 1.00f, 0.45f);

    public static Color Category(LogCategory c) => c switch
    {
        LogCategory.Dano => new Color(1.00f, 0.60f, 0.55f),
        LogCategory.Cura => new Color(0.55f, 1.00f, 0.60f),
        LogCategory.Status => new Color(0.75f, 0.75f, 1.00f),
        LogCategory.Recurso => new Color(0.95f, 0.85f, 0.50f),
        LogCategory.Fase => new Color(1.00f, 0.50f, 1.00f),
        LogCategory.Morte => new Color(1.00f, 0.35f, 0.35f),
        LogCategory.Progressao => Xp,
        LogCategory.Itens => new Color(0.55f, 0.85f, 1f),
        LogCategory.Sistema => new Color(0.95f, 0.80f, 0.45f),
        _ => new Color(0.85f, 0.85f, 0.85f),
    };

    private static string Key(string type) => type.StartsWith("resource.", StringComparison.Ordinal) ? type["resource.".Length..] : type;
}
