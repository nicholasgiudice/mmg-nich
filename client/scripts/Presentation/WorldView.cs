using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Mantém um <see cref="EntityView"/> por unidade da autoridade e desenha cada uma na posição
/// interpolada entre os dois últimos ticks; telegráfos e áreas (também entidades) viram
/// <see cref="TelegraphView"/>/<see cref="AreaView"/>. Conhece só <see cref="IGameAuthority"/>: funciona igual
/// com a simulação local, com o servidor (fase 1) ou com uma autoridade falsa.
/// </summary>
public partial class WorldView : Node2D
{
    private readonly Dictionary<EntityId, EntityView> _views = new();
    private readonly Dictionary<EntityId, EntitySnapshot> _previousById = new();
    private readonly HashSet<EntityId> _seen = new();
    private readonly Dictionary<EntityId, Node2D> _ground = new();
    private ContentCatalog _content = ContentCatalog.Empty;
    private WorldSnapshot? _indexedPrevious;

    private static readonly Color RejectionColor = new(1f, 0.85f, 0.35f);

    /// <summary>Margem (pixels) além do raio ao escolher a unidade sob o cursor.</summary>
    private const float PickMarginPx = 4f;

    /// <summary>Unidade com indicador de alvo selecionado.</summary>
    public EntityId? SelectedEntity { get; set; }

    /// <summary>Definições (nomes e tags de status); vazio = só siglas cruas.</summary>
    public void SetContent(ContentCatalog content) => _content = content;

    /// <summary>Atualiza os nós a partir da autoridade. Chamar uma vez por frame, depois de <c>Advance</c>.</summary>
    public void Render(IGameAuthority authority, IReadOnlyList<GameEvent> events)
    {
        var latest = authority.Latest;
        foreach (var e in events)
        {
            switch (e)
            {
                case EntityRemoved removed:
                    RemoveView(removed.Entity);
                    break;
                case DamageDealt hit when hit.Amount > 0:
                    ShowText(hit.Target, DamageText(hit), Hud.HudTheme.School(hit.School));
                    if (hit.Applied > 0 && _views.TryGetValue(hit.Target, out var hitView))
                    {
                        hitView.Flash();
                    }

                    break;
                case Displaced moved when moved.Distance > 0.1f:
                    var width = _views.TryGetValue(moved.Entity, out var movedView) ? movedView.RadiusPx * 1.6f : 16f;
                    AddChild(TrailView.Create(Units.ToPixels(moved.From), Units.ToPixels(moved.To), width,
                        moved.Kind == DisplacementKind.Knockback ? new Color(1f, 0.6f, 0.4f) : new Color(0.8f, 0.9f, 1f)));
                    break;
                case HealApplied heal when heal.Effective > 0:
                    ShowText(heal.Target, $"+{heal.Effective:0}", Hud.HudTheme.Heal);
                    break;
                case AbilityRejected rejected when rejected.Source == authority.ControlledEntity:
                    var pool = latest?.Entities.FirstOrDefault(s => s.Id == rejected.Source)?.TryGet<ResourcePool>();
                    ShowText(rejected.Source, RejectionText.For(rejected.Reason, pool?.Type), RejectionColor);
                    break;
            }
        }

        if (latest is null)
        {
            return;
        }

        IndexPrevious(authority.Previous);
        var alpha = Math.Clamp(authority.InterpolationFraction, 0f, 1f);
        var now = authority.CurrentTick + alpha;
        var playerFaction = latest.Entities.FirstOrDefault(e => e.Id == authority.ControlledEntity)?.TryGet<FactionTag>()?.Faction
            ?? Faction.Player;
        var seen = _seen;
        seen.Clear();
        foreach (var state in latest.Entities)
        {
            var transform = state.TryGet<Transform>();
            if (transform is null)
            {
                continue;
            }

            if (state.TryGet<TelegraphState>() is { } telegraph)
            {
                seen.Add(state.Id);
                var span = Math.Max(1, telegraph.ResolveAtTick - telegraph.StartTick);
                GetOrCreateGround<TelegraphView>(state.Id).Apply(telegraph, Units.ToPixels(transform.Position),
                    new Godot.Vector2(transform.Facing.X, transform.Facing.Y), (float)((now - telegraph.StartTick) / span),
                    telegraph.SourceFaction != playerFaction);
                continue;
            }

            if (state.TryGet<AreaState>() is { } area)
            {
                seen.Add(state.Id);
                GetOrCreateGround<AreaView>(state.Id).Apply(area, Units.ToPixels(transform.Position), now,
                    area.OwnerFaction != playerFaction);
                continue;
            }

            if (state.TryGet<Health>() is null)
            {
                continue;
            }

            seen.Add(state.Id);
            var view = GetOrCreateView(state.Id);
            var controlled = authority.ControlledEntity == state.Id;
            view.ApplyState(state, controlled, SelectedEntity == state.Id);
            var (chips, marks) = StatusVisuals.From(state.TryGet<StatusEffects>(), _content, now);
            view.ApplyStatus(chips, marks);

            var from = _previousById.TryGetValue(state.Id, out var prev) ? prev.TryGet<Transform>() ?? transform : transform;
            var position = System.Numerics.Vector2.Lerp(from.Position, transform.Position, alpha);
            var facing = System.Numerics.Vector2.Lerp(from.Facing, transform.Facing, alpha);
            var pixels = Units.ToPixels(position);
            view.SetPose(pixels, new Godot.Vector2(facing.X, facing.Y));
        }

        foreach (var id in _views.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            RemoveView(id);
        }

        foreach (var id in _ground.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _ground.Remove(id, out var node);
            node!.QueueFree();
        }
    }

    private T GetOrCreateGround<T>(EntityId id)
        where T : Node2D, new()
    {
        if (_ground.TryGetValue(id, out var node) && node is T typed)
        {
            return typed;
        }

        typed = new T { Name = $"{typeof(T).Name}{id.Value}" };
        _ground[id] = typed;
        AddChild(typed);
        return typed;
    }

    /// <summary>"-23", "-15 (8 absorvido)" ou "Absorvido 12" quando o escudo segurou tudo.</summary>
    private static string DamageText(DamageDealt hit)
    {
        var taken = hit.Amount - hit.Absorbed;
        if (taken <= 0)
        {
            return $"Absorvido {hit.Absorbed:0}";
        }

        return hit.Absorbed > 0 ? $"-{taken:0} ({hit.Absorbed:0} absorvido)" : $"-{taken:0}";
    }

    /// <summary>Posição desenhada (pixels) de uma unidade neste frame.</summary>
    public bool TryGetDrawnPosition(EntityId id, out Godot.Vector2 pixels)
    {
        if (_views.TryGetValue(id, out var view))
        {
            pixels = view.Position;
            return true;
        }

        pixels = default;
        return false;
    }

    /// <summary>Unidade viva desenhada sob o ponto (pixels); a mais próxima se houver várias.</summary>
    public EntityId? PickAt(Godot.Vector2 pixels) =>
        _views.Values
            .Where(v => v.IsAlive && v.Position.DistanceTo(pixels) <= v.RadiusPx + PickMarginPx)
            .OrderBy(v => v.Position.DistanceTo(pixels))
            .Select(v => (EntityId?)v.Id)
            .FirstOrDefault();

    /// <summary>Texto flutuante acima de uma unidade; acompanha a unidade e some sozinho.</summary>
    public void ShowText(EntityId id, string text, Color color)
    {
        if (!_views.TryGetValue(id, out var view))
        {
            return;
        }

        var label = FloatingText.Create(text, color);
        label.Position = new Godot.Vector2(0, -view.RadiusPx - 18);
        view.AddChild(label);
    }

    private void IndexPrevious(WorldSnapshot? previous)
    {
        if (ReferenceEquals(previous, _indexedPrevious))
        {
            return;
        }

        _indexedPrevious = previous;
        _previousById.Clear();
        if (previous is null)
        {
            return;
        }

        foreach (var state in previous.Entities)
        {
            _previousById[state.Id] = state;
        }
    }

    private EntityView GetOrCreateView(EntityId id)
    {
        if (!_views.TryGetValue(id, out var view))
        {
            view = EntityView.Create(id);
            _views.Add(id, view);
            AddChild(view);
        }

        return view;
    }

    private void RemoveView(EntityId id)
    {
        if (_views.Remove(id, out var view))
        {
            view.QueueFree();
        }
    }
}
