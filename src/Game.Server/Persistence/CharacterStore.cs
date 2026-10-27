using Game.Persistence;

namespace Game.Server.Persistence;

/// <summary>Acesso do servidor aos personagens salvos (T-212). Separado do repositório para os testes simularem banco lento ou fora.</summary>
public interface ICharacterStore
{
    Task<Character?> LoadAsync(Guid characterId, CancellationToken ct);

    /// <summary>Grava (uma transação: um UPDATE com checagem de versão); em <see cref="SaveStatus.Saved"/> a entidade recebe a versão nova.</summary>
    Task<SaveStatus> SaveAsync(Character character, CancellationToken ct);
}

public sealed class RepositoryCharacterStore(GameRepository repository) : ICharacterStore
{
    public Task<Character?> LoadAsync(Guid characterId, CancellationToken ct) => repository.LoadCharacterAsync(characterId, ct);

    public Task<SaveStatus> SaveAsync(Character character, CancellationToken ct) => repository.SaveCharacterAsync(character, ct);
}
