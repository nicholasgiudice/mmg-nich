namespace Game.Persistence;

/// <summary>Conta de jogador (T-210). O e-mail é guardado normalizado (minúsculas, sem espaços nas pontas).</summary>
public sealed class Account
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public List<Character> Characters { get; set; } = [];
}

/// <summary>
/// Personagem (T-210). Colunas relacionais para o que é consultado (nome, classe, nível, zona); o estado de progressão
/// (atributos, NH, inventário, equipamento — T-203/T-204) fica em <see cref="Progression"/> (jsonb), com a versão do formato
/// em <see cref="ProgressionVersion"/>. <see cref="Version"/> é a coluna de sistema <c>xmin</c> do PostgreSQL, usada na
/// concorrência otimista: salvar com uma versão velha falha.
/// </summary>
public sealed class Character
{
    public const int MaxNameLength = 24;

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public required string Name { get; set; }

    /// <summary>Nome em maiúsculas invariantes, com índice único (nomes não diferenciam maiúsculas).</summary>
    public string NormalizedName { get; set; } = "";

    public required string ClassId { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public required string ZoneId { get; set; }

    /// <summary>Posição na zona, em tiles.</summary>
    public float PositionX { get; set; }
    public float PositionY { get; set; }

    public int ProgressionVersion { get; set; } = 1;

    /// <summary>Documento JSON (jsonb) do estado de progressão; o formato pertence a quem grava (T-203/T-204).</summary>
    public string Progression { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public uint Version { get; set; }

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();
}
