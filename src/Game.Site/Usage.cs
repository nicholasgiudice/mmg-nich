using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Site;

/// <summary>Quem usa uma habilidade, status, área ou monstro: o rótulo de quem aplica e os NH (vazio = sem NH).</summary>
public sealed record UseSource(string Kind, string Id, string? Detail, IReadOnlyList<int> Ranks);

/// <summary>
/// Índices de uso montados a partir dos dados: quais habilidades as classes e os monstros usam, quem aplica cada status,
/// quem cria cada área e quem invoca cada monstro. O que nenhum personagem nem monstro usa fica de fora das páginas
/// normais e vai para "Em aberto" (pontos em aberto dos dados).
/// </summary>
public sealed class Usage
{
    private readonly GameDatabase _db;

    /// <summary>Habilidade → classes que a têm (ataque básico, habilidade ou consumível).</summary>
    public Dictionary<string, List<string>> AbilityClasses { get; } = new(StringComparer.Ordinal);

    /// <summary>Habilidade → monstros que a têm.</summary>
    public Dictionary<string, List<string>> AbilityMonsters { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, List<UseSource>> StatusSources { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<UseSource>> AreaSources { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<UseSource>> SummonSources { get; } = new(StringComparer.Ordinal);

    public Usage(GameDatabase db)
    {
        _db = db;
        foreach (var cls in db.Classes.Values)
        {
            foreach (var id in new[] { cls.BasicAttack }.Concat(cls.Abilities))
            {
                Add(AbilityClasses, id, cls.Id);
            }
        }

        foreach (var m in db.Monsters.Values)
        {
            foreach (var id in m.Abilities.Concat(m.Phases.SelectMany(p => p.Abilities ?? [])).Distinct())
            {
                Add(AbilityMonsters, id, m.Id);
            }
        }

        // Efeitos das habilidades usadas, NH a NH (herança já resolvida).
        foreach (var a in db.Abilities.Values.Where(a => IsUsed(a.Id)))
        {
            for (var rank = 1; rank <= a.HighestRank; rank++)
            {
                var resolved = AbilityRanks.Resolve(a, rank);
                var ranks = a.HighestRank > 1 ? new[] { rank } : [];
                Walk(resolved.Effects, new UseSource("ability", a.Id, null, ranks));
            }
        }

        foreach (var m in db.Monsters.Values)
        {
            foreach (var p in m.Phases)
            {
                Walk(p.OnEnter, new UseSource("phase", m.Id, p.Id, []));
            }
        }

        Merge(StatusSources);
        Merge(AreaSources);
        Merge(SummonSources);
    }

    public bool IsUsed(string abilityId) =>
        AbilityClasses.ContainsKey(abilityId) || AbilityMonsters.ContainsKey(abilityId)
        || _db.Items.Values.Any(i => i.UseAbility == abilityId); // T-320: habilidade de consumível

    public bool IsStatusUsed(string id) => StatusSources.ContainsKey(id);

    public bool IsAreaUsed(string id) => AreaSources.ContainsKey(id);

    /// <summary>Habilidades nos dados que ninguém usa (ex.: Armadilha de Contenção fora da lista do Arqueiro).</summary>
    public IEnumerable<AbilityDefinition> UnusedAbilities => _db.Abilities.Values.Where(a => !IsUsed(a.Id)).OrderBy(a => a.Id, StringComparer.Ordinal);

    public IEnumerable<StatusDefinition> UnusedStatuses => _db.Statuses.Values.Where(s => !IsStatusUsed(s.Id)).OrderBy(s => s.Id, StringComparer.Ordinal);

    public IEnumerable<AreaDefinition> UnusedAreas => _db.Areas.Values.Where(a => !IsAreaUsed(a.Id)).OrderBy(a => a.Id, StringComparer.Ordinal);

    private readonly HashSet<string> _walkingAreas = new(StringComparer.Ordinal);

    private void Walk(IEnumerable<EffectDefinition> effects, UseSource source)
    {
        foreach (var e in effects)
        {
            switch (e)
            {
                case ApplyStatusEffect s:
                    Add(StatusSources, s.Status, source);
                    if (_db.Statuses.TryGetValue(s.Status, out var st))
                    {
                        foreach (var p in st.Periodic)
                        {
                            Walk(p.Effects, source);
                        }
                    }

                    break;
                case SpawnAreaEffect a:
                    Add(AreaSources, a.Area, source);
                    if (_db.Areas.TryGetValue(a.Area, out var area) && _walkingAreas.Add(a.Area))
                    {
                        Walk(area.Effects, new UseSource("area", area.Id, null, []));
                        _walkingAreas.Remove(a.Area);
                    }

                    break;
                case SummonEffect m:
                    Add(SummonSources, m.Monster, source);
                    break;
                case ChainEffect c:
                    Walk(c.Effects, source);
                    break;
                case ExtraTargetsEffect x:
                    Walk(x.Effects, source);
                    break;
            }
        }
    }

    private static void Add<T>(Dictionary<string, List<T>> map, string key, T value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = list = [];
        }

        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }

    /// <summary>Junta as entradas da mesma fonte (habilidade, fase, área) somando os NH.</summary>
    private static void Merge(Dictionary<string, List<UseSource>> map)
    {
        foreach (var key in map.Keys.ToList())
        {
            map[key] = map[key]
                .GroupBy(s => (s.Kind, s.Id, s.Detail))
                .Select(g => new UseSource(g.Key.Kind, g.Key.Id, g.Key.Detail,
                    g.SelectMany(s => s.Ranks).Distinct().OrderBy(r => r).ToList()))
                .ToList();
        }
    }
}
