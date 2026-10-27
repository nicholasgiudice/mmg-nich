using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace Game.Auth;

/// <summary>
/// Token compacto assinado com HMAC-SHA256 (T-211): <c>base64url(JSON) "." base64url(assinatura)</c>. Não é criptografado —
/// quem tem o token lê o conteúdo; só não consegue alterá-lo sem a chave. O conteúdo leva um tipo (<c>typ</c>) para um token
/// de um propósito nunca valer como outro, mesmo que alguém configure a mesma chave nos dois.
/// </summary>
public static class SignedToken
{
    /// <summary>Tamanho mínimo da chave, em bytes.</summary>
    public const int MinKeyBytes = 32;

    public static string Sign<T>(T payload, byte[] key)
    {
        CheckKey(key);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload);
        return Base64Url.EncodeToString(body) + "." + Base64Url.EncodeToString(HMACSHA256.HashData(key, body));
    }

    /// <summary>Lê o conteúdo se a assinatura confere; null se o token está malformado ou foi alterado.</summary>
    public static T? Read<T>(string? token, byte[] key)
        where T : class
    {
        CheckKey(key);
        if (string.IsNullOrEmpty(token) || token.Length > 4096 || token.Split('.') is not [var bodyPart, var signaturePart])
        {
            return null;
        }

        byte[] body, signature;
        try
        {
            body = Base64Url.DecodeFromChars(bodyPart);
            signature = Base64Url.DecodeFromChars(signaturePart);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(signature, HMACSHA256.HashData(key, body)))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Chave em base64 (variável de ambiente); lança com mensagem clara se falta ou é curta.</summary>
    public static byte[] KeyFromBase64(string? base64, string name)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException($"Defina {name} (base64 de pelo menos {MinKeyBytes} bytes; ex.: openssl rand -base64 32).");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{name} não é base64 válido.");
        }

        return key.Length >= MinKeyBytes ? key : throw new InvalidOperationException($"{name} tem {key.Length} bytes; o mínimo é {MinKeyBytes}.");
    }

    private static void CheckKey(byte[] key)
    {
        if (key.Length < MinKeyBytes)
        {
            throw new ArgumentException($"Chave com menos de {MinKeyBytes} bytes.", nameof(key));
        }
    }
}
