using Game.Core.Combat.Ai.Profiles;

namespace Game.Core.Combat.Ai;

/// <summary>
/// Perfis de IA por id (<c>MonsterDefinition.Ai.Id</c>). Para adicionar um perfil novo (ex.: fases de boss, T-012):
/// crie a classe em <c>Combat/Ai/Profiles/</c> e acrescente UMA linha em <see cref="CreateDefault"/>.
/// </summary>
public sealed class AiProfileRegistry
{
    private readonly Dictionary<string, IAiProfile> _profiles = new(StringComparer.Ordinal);

    public AiProfileRegistry Register(string id, IAiProfile profile)
    {
        if (!_profiles.TryAdd(id, profile))
        {
            throw new InvalidOperationException($"AI profile '{id}' already registered");
        }

        return this;
    }

    public IAiProfile Get(string id) =>
        _profiles.TryGetValue(id, out var p)
            ? p
            : throw new InvalidOperationException($"Unknown AI profile '{id}' (register it in AiProfileRegistry.CreateDefault)");

    public static AiProfileRegistry CreateDefault() => new AiProfileRegistry()
        .Register("ai.none", new PassiveProfile())
        .Register("ai.melee_chaser", new MeleeChaserProfile())
        .Register("ai.pack", new PackProfile())
        .Register("ai.ranged_kiter", new RangedKiterProfile())
        .Register("ai.swarm", new SwarmProfile())
        .Register("ai.guardian", new GuardianProfile());
}
