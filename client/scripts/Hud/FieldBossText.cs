using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;

namespace MmoRpgClient.Hud;

/// <summary>
/// Aviso de chefe de campo (T-540, evento <see cref="FieldBossAnnounced"/> da T-503): o texto do banner no alto da tela e a
/// linha do log. Tudo sai do nome do monstro (<c>data/monsters.json</c>) e da subárea do evento, nada fixo no código.
/// O gênero do nome (o artigo e o particípio) vem do fim da primeira palavra, como em português: "Bruxa do Brejo" é "A" e
/// "derrotada", "Capataz Forjado" e "Golem de Pedra" são "O" e "derrotado". Um nome que fuja disso (ex.: "Vigia") pede um
/// campo de gênero nos dados.
/// </summary>
public static class FieldBossText
{
    /// <summary>Texto do banner e da linha do log: "A Bruxa do Brejo despertou na Cabana da Bruxa" / "A Bruxa do Brejo foi derrotada".</summary>
    public static string Line(ContentCatalog content, FieldBossAnnounced e)
    {
        var name = content.Monsters.TryGetValue(e.MonsterId, out var monster) ? monster.Name : e.MonsterId;
        var feminine = IsFeminine(name);
        var subject = $"{(feminine ? "A" : "O")} {name}";
        if (!e.Spawned)
        {
            return $"{subject} foi {(feminine ? "derrotada" : "derrotado")}";
        }

        return string.IsNullOrWhiteSpace(e.Area) ? $"{subject} despertou" : $"{subject} despertou {In(e.Area)}";
    }

    /// <summary>"na Cabana da Bruxa", "no Brejo Fundo", "nas Minas", "em Passo" (palavra de fim sem gênero claro).</summary>
    public static string In(string place)
    {
        var first = place.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        var preposition = first.EndsWith("as", StringComparison.Ordinal) ? "nas"
            : first.EndsWith("os", StringComparison.Ordinal) ? "nos"
            : first.EndsWith('a') ? "na"
            : first.EndsWith('o') ? "no"
            : "em";
        return $"{preposition} {place}";
    }

    private static bool IsFeminine(string name)
    {
        var first = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return first.EndsWith('a') || first.EndsWith("ã");
    }

    /// <summary>
    /// Registra a linha do log do evento (categoria Fase), se o Core ainda não tiver formatador para ele. Os dois
    /// <see cref="CombatLogRecorder"/> do cliente (local e rede) usam <see cref="Formatters"/>.
    /// </summary>
    public static CombatLogFormatters Register(CombatLogFormatters formatters)
    {
        if (!formatters.IsRegistered<FieldBossAnnounced>())
        {
            formatters.Register<FieldBossAnnounced>((c, e) => c.Entry(e, LogCategory.Fase, Line(c.Content, e)));
        }

        return formatters;
    }

    /// <summary>Formatadores padrão do Core mais os do cliente.</summary>
    public static CombatLogFormatters Formatters() => Register(CombatLogFormatters.CreateDefault());
}
