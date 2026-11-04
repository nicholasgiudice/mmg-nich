using Godot;
using MmoRpgClient.Input;

namespace MmoRpgClient.Hud;

/// <summary>
/// Barra de atalhos guardada por personagem (T-081) em <c>user://hotbar.cfg</c>: a chave é o personagem (id da conta online,
/// nome no login de desenvolvimento, classe no jogo local) e o valor, os ids da barra (<see cref="HotbarLayout.Serialize"/>).
/// </summary>
public static class HotbarStore
{
    private const string File = "user://hotbar.cfg";
    private const string Section = "hotbar";

    /// <summary>Barra guardada do personagem, ou null se não há.</summary>
    public static string?[]? Load(string character)
    {
        var config = new ConfigFile();
        var key = Key(character);
        if (config.Load(File) != Error.Ok || !config.HasSectionKey(Section, key))
        {
            return null;
        }

        return HotbarLayout.Parse((string)config.GetValue(Section, key, ""));
    }

    public static void Save(string character, HotbarLayout layout)
    {
        var config = new ConfigFile();
        config.Load(File); // sem arquivo ainda: começa vazio
        config.SetValue(Section, Key(character), layout.Serialize());
        config.Save(File);
    }

    /// <summary>Chave segura para o arquivo: letras, dígitos, <c>-</c>, <c>_</c> e <c>.</c>; o resto vira <c>_</c>.</summary>
    private static string Key(string character) =>
        new(character.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_').ToArray());
}
