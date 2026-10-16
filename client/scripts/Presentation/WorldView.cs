using Game.Core.Authority;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Mantém um <see cref="EntityView"/> por unidade da autoridade e desenha cada uma na posição
/// interpolada entre os dois últimos ticks. Conhece só <see cref="IGameAuthority"/>: funciona igual
/// com a simulação local, com o servidor (fase 1) ou com uma autoridade falsa.
/// </summary>
public partial class WorldView : Node2D
{
    private readonly Dictionary<EntityId, EntityView> _views = new();
    private readonly Dictionary<EntityId, EntitySnapshot> _previousById = new();
    private readonly HashSet<EntityId> _seen = new();
    private WorldSnapshot? _indexedPrevious;

    /// <summary>Câmera que segue a entidade controlada (opcional).</summary>
    public Camera2D? Camera { get; set; }

    /// <summary>Atualiza os nós a partir da autoridade. Chamar uma vez por frame, depois de <c>Advance</c>.</summary>
    public void Render(IGameAuthority authority, IReadOnlyList<GameEvent> events)
    {
        foreach (var e in events)
        {
            if (e is EntityRemoved removed)
            {
                RemoveView(removed.Entity);
            }
        }

        var latest = authority.Latest;
        if (latest is null)
        {
            return;
        }

        IndexPrevious(authority.Previous);
        var alpha = Math.Clamp(authority.InterpolationFraction, 0f, 1f);
        var seen = _seen;
        seen.Clear();
        foreach (var state in latest.Entities)
        {
            var transform = state.TryGet<Transform>();
            if (transform is null || state.TryGet<Health>() is null)
            {
                continue; // não é unidade (telegráfos, áreas...): fica para a T-023
            }

            seen.Add(state.Id);
            var view = GetOrCreateView(state.Id);
            var controlled = authority.ControlledEntity == state.Id;
            view.ApplyState(state, controlled);

            var from = _previousById.TryGetValue(state.Id, out var prev) ? prev.TryGet<Transform>() ?? transform : transform;
            var position = System.Numerics.Vector2.Lerp(from.Position, transform.Position, alpha);
            var facing = System.Numerics.Vector2.Lerp(from.Facing, transform.Facing, alpha);
            var pixels = Units.ToPixels(position);
            view.SetPose(pixels, new Godot.Vector2(facing.X, facing.Y));

            if (controlled && Camera is not null)
            {
                Camera.Position = pixels;
            }
        }

        foreach (var id in _views.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            RemoveView(id);
        }
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
