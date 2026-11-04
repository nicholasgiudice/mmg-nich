using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Informações de desenvolvimento na tela (T-085): texto do canto (versão, FPS, tick, rede), linha de ajuda, log de
/// combate e grade de <c>--debug-grid</c>. Começam escondidas; Ctrl+F alterna, e a escolha fica em
/// <c>user://settings.cfg</c> (o mesmo arquivo da intensidade dos efeitos).
/// </summary>
public static class DebugView
{
    private const string File = "user://settings.cfg";
    private const string Section = "debug";
    private const string Key = "visible";

    private static bool? s_visible;

    public static bool Visible
    {
        get => s_visible ??= Load();
        set
        {
            s_visible = value;
            var config = new ConfigFile();
            config.Load(File); // sem arquivo ainda: começa vazio
            config.SetValue(Section, Key, value);
            config.Save(File);
        }
    }

    /// <summary>Com <c>--debug-grid</c> o debug começa visível só nesta execução (não grava).</summary>
    public static void ForceVisible() => s_visible = true;

    private static bool Load()
    {
        var config = new ConfigFile();
        return config.Load(File) == Error.Ok && (bool)config.GetValue(Section, Key, false);
    }
}
