using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Intensidade dos efeitos (T-054), escolhida na pausa e lembrada em <c>user://settings.cfg</c>. Reduzida: só os
/// impactos e projéteis que envolvem o jogador, arte das áreas e escudos mais transparentes. Telegráfos e contornos
/// das áreas não mudam.
/// </summary>
public static class EffectSettings
{
    private const string File = "user://settings.cfg";
    private const string Section = "video";
    private const string Key = "effects";

    private static bool? s_reduced;

    public static bool Reduced
    {
        get => s_reduced ??= LoadReduced();
        set
        {
            s_reduced = value;
            var config = new ConfigFile();
            config.Load(File); // sem arquivo ainda: começa vazio
            config.SetValue(Section, Key, value ? "reduced" : "normal");
            config.Save(File);
        }
    }

    /// <summary>Opacidade da arte das áreas no chão.</summary>
    public static float AreaAlpha => Reduced ? 0.35f : 0.8f;

    /// <summary>Opacidade da bolha de escudo.</summary>
    public static float ShieldAlpha => Reduced ? 0.4f : 0.8f;

    /// <summary><c>--effects=reduced|normal</c> (depuração) vale só para esta execução.</summary>
    public static void Override(string? value)
    {
        if (value is "reduced" or "normal")
        {
            s_reduced = value == "reduced";
        }
    }

    public static string Label => Reduced ? "Efeitos: reduzidos" : "Efeitos: normais";

    private static bool LoadReduced()
    {
        var config = new ConfigFile();
        return config.Load(File) == Error.Ok && (string)config.GetValue(Section, Key, "normal") == "reduced";
    }
}
