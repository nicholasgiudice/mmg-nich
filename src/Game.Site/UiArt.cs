namespace Game.Site;

/// <summary>
/// Arte de interface do cliente usada no visual do site (fundo da tela de login e kit pedra e ouro, T-088/T-089/T-097):
/// copiada de <c>client/assets/art/ui</c> para <c>assets/ui</c>, onde o <c>style.css</c> a referencia. Arquivo ausente só
/// gera aviso: o site continua legível sem ele.
/// </summary>
public static class UiArt
{
    public const string PublishedDir = "assets/ui";

    /// <summary>Arquivos de <c>client/assets/art/ui</c> que o <c>style.css</c> usa.</summary>
    public static readonly IReadOnlyList<string> Files =
    [
        "login_background.png", // topo da página inicial
        "panel.png", // moldura dos painéis (apresentação, download)
        "frame_member.png", // moldura fina dos cartões e das caixas de referência
        "divider.png", // linha sob os títulos de seção
        "button_normal.png", // botões
        "button_hover.png",
        "button_pressed.png",
        "crown.png", // marca ao lado do nome do jogo
    ];

    public static void Copy(string artDir, string outDir, List<string> warnings)
    {
        var target = Path.Combine(outDir, PublishedDir);
        Directory.CreateDirectory(target);
        foreach (var file in Files)
        {
            var source = Path.Combine(artDir, "ui", file);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(target, file), overwrite: true);
            }
            else
            {
                warnings.Add($"arte de interface ui/{file} não encontrada: o site fica sem ela");
            }
        }
    }
}
