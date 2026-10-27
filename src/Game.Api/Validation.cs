using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace Game.Api;

/// <summary>Regras de e-mail, senha e nome de personagem (T-211).</summary>
public static partial class Validation
{
    public const int MinPassword = 8;
    public const int MaxPassword = 128;
    public const int MinName = 3;
    public const int MaxName = 16;

    public static bool IsValidEmail(string? email) =>
        email is not null && email.Trim().Length <= 254 && EmailPattern().IsMatch(email.Trim());

    public static bool IsValidPassword(string? password) => password is { Length: >= MinPassword and <= MaxPassword };

    /// <summary>
    /// Nome normalizado (NFC, sem espaços nas pontas) se vale: 3 a 16 letras latinas (com acento) e dígitos, começando por
    /// letra. Só alfabeto latino para evitar nomes iguais na tela com letras de outros alfabetos.
    /// </summary>
    public static string? NormalizeName(string? name)
    {
        if (name is null)
        {
            return null;
        }

        var normalized = name.Trim().Normalize(NormalizationForm.FormC);
        return NamePattern().IsMatch(normalized) ? normalized : null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ0-9]{2,15}$")]
    private static partial Regex NamePattern();
}

/// <summary>Hash de senha com o <see cref="PasswordHasher{TUser}"/> do ASP.NET Identity (PBKDF2-HMAC-SHA512, sal aleatório).</summary>
public sealed class Passwords
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    // Hash de uma senha qualquer: o login de e-mail inexistente verifica contra ele e demora o mesmo que o de senha errada.
    private readonly string _dummyHash;

    public Passwords() => _dummyHash = Hash(Guid.NewGuid().ToString());

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public bool Verify(string? hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(User, hash ?? _dummyHash, password);
        return hash is not null && result != PasswordVerificationResult.Failed;
    }
}
