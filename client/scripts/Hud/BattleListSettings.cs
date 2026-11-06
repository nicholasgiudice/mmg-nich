using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Lista de batalha visível ou escondida (tecla B, T-312), lembrada em <c>user://settings.cfg</c>.</summary>
public static class BattleListSettings
{
    private const string File = "user://settings.cfg";
    private const string Section = "hud";
    private const string Key = "battle_list";

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

    private static bool Load()
    {
        var config = new ConfigFile();
        return config.Load(File) != Error.Ok || (bool)config.GetValue(Section, Key, true);
    }
}
