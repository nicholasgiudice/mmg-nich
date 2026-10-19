using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Game.Data;

/// <summary>
/// Impressão digital estável do conteúdo (T-101): cliente e servidor só jogam juntos com o mesmo valor.
/// Usa o JSON canônico (sem espaços, comentários nem diferença de quebra de linha) e o nome de cada
/// arquivo, em ordem; o mesmo conteúdo dá o mesmo valor em Windows e Linux.
/// </summary>
public static class ContentFingerprint
{
    public static string Compute(IEnumerable<(string Name, string Json)> sources)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var ordered = sources
            .Select(s => (Name: s.Name.Replace('\\', '/'), s.Json))
            .OrderBy(s => s.Name, StringComparer.Ordinal);
        foreach (var (name, json) in ordered)
        {
            sha.AppendData(Encoding.UTF8.GetBytes(name + "\n"));
            sha.AppendData(Canonical(json));
            sha.AppendData("\n"u8);
        }

        return Convert.ToHexString(sha.GetHashAndReset())[..16].ToLowerInvariant();
    }

    private static byte[] Canonical(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            document.WriteTo(writer);
        }

        return buffer.ToArray();
    }
}
