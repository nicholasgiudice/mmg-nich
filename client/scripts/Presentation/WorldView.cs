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
    private (string Text, ulong AtMs) _lastRejection;

    /// <summary>A mesma mensagem de rejeição não se repete antes disto (botão mantido fora de alcance).</summary>
    private const ulong RejectionRepeatMs = 1000;
    private WorldSnapshot? _indexedPrevious;

    private static readonly Color RejectionColor = new(1f, 0.85f, 0.35f);
    private static readonly Color InterruptColor = new(1f, 0.5f, 0.3f);

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
                case CastInterrupted cut when cut.Reason != CastInterruptReason.Died:
                    var own = cut.Entity == authority.ControlledEntity;
                    ShowText(cut.Entity, cut.Reason switch
                    {
                        CastInterruptReason.Moved when own => "Interrompido — você se moveu",
                        CastInterruptReason.Disabled => "Impedido",
                        CastInterruptReason.Cancelled => "Cancelado",
                        _ => "Interrompido",
                    }, InterruptColor);
                    break;
                case AbilityExecuted used when MultiHit(latest, used.Source, used.AbilityId) is { } first:
                    // T-064: habilidade com vários acertos — "×3" uma vez e o rastro de cada acerto em alvo único.
                    ShowText(used.Source, $"{first.Name} ×{first.HitCount}", HitCountColor);
                    ShotTrails(used.Source, used.Targets, first);
                    break;
                case AbilityHit extra:
                    if (_content.Abilities.TryGetValue(extra.AbilityId, out var extraDef))
                    {
                        ShotTrails(extra.Source, extra.Targets, extraDef);
                    }

                    break;
                case Game.Core.Combat.Effects.ChainJumped jump
                    when _views.TryGetValue(jump.From, out var fromView) && _views.TryGetValue(jump.To, out var toView):
                    AddChild(TrailView.Create(fromView.Position, toView.Position, 7f, ChainColor, lifetime: 0.8));
                    break;
                case EntityRevived revived:
                    ShowText(revived.Entity, "Levantado!", Hud.HudTheme.Heal);
                    break;
                case HealApplied heal when heal.Effective > 0:
                    ShowText(heal.Target, $"+{heal.Effective:0}", Hud.HudTheme.Heal);
                    break;
                case AbilityRejected rejected when rejected.Source == authority.ControlledEntity:
                    var pool = latest?.Entities.FirstOrDefault(s => s.Id == rejected.Source)?.TryGet<ResourcePool>();
                    var text = RejectionText.For(rejected.Reason, pool?.Type);
                    var nowMs = Time.GetTicksMsec();
                    if (text != _lastRejection.Text || nowMs - _lastRejection.AtMs >= RejectionRepeatMs)
                    {
                        _lastRejection = (text, nowMs);
                        ShowText(rejected.Source, text, RejectionColor);
                    }

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
    private static readonly Color HitCountColor = new(1f, 0.9f, 0.55f);
    private static readonly Color ShotColor = new(1f, 0.95f, 0.75f);
    private static readonly Color ChainColor = new(1f, 0.85f, 0.3f);

    /// <summary>Definição no NH da fonte, se a habilidade tem mais de um acerto (T-064); senão null.</summary>
    private AbilityDefinition? MultiHit(WorldSnapshot? latest, EntityId source, string abilityId)
    {
        var rank = latest?.Entities.FirstOrDefault(s => s.Id == source)?.TryGet<AbilityState>()?.RankOf(abilityId) ?? 1;
        return _content.AbilityAt(abilityId, rank) is { HitCount: > 1 } def ? def : null;
    }

    /// <summary>Rastro fino da fonte até cada alvo, só para alvo único (flechas, projéteis): áreas já têm telegráfo.</summary>
    private void ShotTrails(EntityId source, IReadOnlyList<EntityId> targets, AbilityDefinition def)
    {
        if (def.Targeting is not UnitTargeting || !_views.TryGetValue(source, out var from))
        {
            return;
        }

        foreach (var t in targets)
        {
            if (t != source && _views.TryGetValue(t, out var to))
            {
                AddChild(TrailView.Create(from.Position, to.Position, 4f, ShotColor));
            }
        }
    }

    public void ShowText(EntityId id, string text, Color color)
    {
        if (!_views.TryGetValue(id, out var view))
        {
            return;
        }

        // T-064: textos em sequência rápida (flechas extras, giros) saem lado a lado em vez de um por cima do outro.
        var recent = view.GetChildren().OfType<FloatingText>().Count(t => t.Age < 0.4);
        var side = recent == 0 ? 0 : (recent % 2 == 1 ? 1 : -1) * ((recent + 1) / 2) * 26f;
        var label = FloatingText.Create(text, color);
        label.Position = new Godot.Vector2(side, -view.RadiusPx - 18 - Math.Min(recent, 4) * 5f);
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
