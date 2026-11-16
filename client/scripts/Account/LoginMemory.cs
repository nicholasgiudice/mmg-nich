#if GODOT
using Godot;
#endif

namespace MmoRpgClient.Account;

/// <summary>Chaves de texto da seção <c>[account]</c> do arquivo de configurações (null = chave ausente).</summary>
public interface IAccountSettings
{
    string? Get(string key);

    /// <summary>Grava a chave; null apaga.</summary>
    void Set(string key, string? value);
}

/// <summary>
/// "Lembrar e-mail" do login (T-381): o e-mail e o estado da caixa ficam na seção <c>[account]</c> de
/// <c>user://settings.cfg</c>. A senha nunca é gravada. Sem nada gravado, a caixa começa desmarcada.
/// </summary>
public sealed class LoginMemory(IAccountSettings settings)
{
    public const string EmailKey = "email";
    public const string RememberKey = "remember_email";

    /// <summary>Estado da caixa ao abrir a tela: o gravado; sem estado gravado, marcada só se há e-mail gravado.</summary>
    public bool Remember => settings.Get(RememberKey) is { } stored ? stored == "true" : Email is not null;

    /// <summary>E-mail gravado (null se não há).</summary>
    public string? Email => settings.Get(EmailKey) is { Length: > 0 } email ? email : null;

    /// <summary>Depois de entrar com sucesso: marcada grava o e-mail; desmarcada apaga o que havia.</summary>
    public void AfterLogin(bool remember, string email)
    {
        var trimmed = email.Trim();
        settings.Set(RememberKey, remember ? "true" : "false");
        settings.Set(EmailKey, remember && trimmed.Length > 0 ? trimmed : null);
    }

#if GODOT
    /// <summary>A memória sobre <c>user://settings.cfg</c>, o mesmo arquivo do <c>EffectSettings</c> e do <c>BattleListSettings</c>.</summary>
    public static LoginMemory FromSettingsFile() => new(new ConfigFileSettings());

    private sealed class ConfigFileSettings : IAccountSettings
    {
        private const string File = "user://settings.cfg";
        private const string Section = "account";

        public string? Get(string key)
        {
            var config = new ConfigFile();
            return config.Load(File) == Error.Ok && config.HasSectionKey(Section, key) ? config.GetValue(Section, key).AsString() : null;
        }

        public void Set(string key, string? value)
        {
            var config = new ConfigFile();
            config.Load(File); // sem arquivo ainda: começa vazio
            if (value is not null)
            {
                config.SetValue(Section, key, value);
            }
            else if (config.HasSectionKey(Section, key))
            {
                config.EraseSectionKey(Section, key);
            }

            config.Save(File);
        }
    }
#endif
}
