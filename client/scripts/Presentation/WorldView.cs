using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Items;
using Game.Core.Combat.Shops;
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
/// <see cref="TelegraphView"/>/<see cref="AreaView"/>, e itens no chão viram <see cref="GroundItemView"/> (T-222). Conhece só <see cref="IGameAuthority"/>: funciona igual
/// com a simulação local, com o servidor (fase 1) ou com uma autoridade falsa.
/// </summary>
public partial class WorldView : Node2D
{
    private readonly Dictionary<EntityId, EntityView> _views = new();
    private readonly Dictionary<EntityId, EntitySnapshot> _previousById = new();
    private readonly HashSet<EntityId> _seen = new();
    private readonly Dictionary<EntityId, Node2D> _ground = new();

    /// <summary>Itens no chão desenhados neste frame (T-222): posição na tela, se o jogador tem direito e se alcança.</summary>
    private readonly Dictionary<EntityId, (Godot.Vector2 Pixels, bool Mine, bool InReach)> _items = new();

    /// <summary>NPCs desenhados neste frame (T-323): posição dos pés na tela e o componente (nome, loja).</summary>
    private readonly Dictionary<EntityId, (Godot.Vector2 Pixels, NpcInfo Info)> _npcs = new();

    /// <summary>Distância (tiles) entre itens que caíram no mesmo ponto, espalhados em volta dele.</summary>
    private const float ItemSpread = 0.55f;
    private ContentCatalog _content = ContentCatalog.Empty;
    private SpriteCatalog _sprites = SpriteCatalog.Empty;
    private EffectCatalog _effects = EffectCatalog.Empty;

    /// <summary>Linhas dos status que ligam duas unidades (T-412, Interceptação).</summary>
    private LinkView? _linkView;
    private readonly List<(EntityId Protected, EntityId Protector, Color Color)> _links = new();

    /// <summary>Ponto mirado e alvo de cada uso começado neste lote de eventos (T-434), por quem usou e habilidade.</summary>
    private readonly Dictionary<(EntityId Source, string AbilityId), (System.Numerics.Vector2? Aim, EntityId? Target)> _aims = new();

    /// <summary>Projéteis desenhados neste frame: o impacto no alvo espera a chegada (s).</summary>
    private readonly Dictionary<(EntityId Source, EntityId Target), double> _flights = new();

    /// <summary>Disparos desenhados neste frame por fonte (T-444): cada orbe sai de um ponto.</summary>
    private readonly Dictionary<EntityId, int> _shotsThisFrame = new();

    /// <summary>Raios das canalizações (T-444).</summary>
    private BeamView? _beamView;
    private readonly List<(EntityId Source, EntityId Target, ChannelBeam Beam, float Strength)> _beams = new();

    /// <summary>Meteoros sobre os avisos (T-444): a pedra caindo e o que é preciso para a explosão quando o aviso some.</summary>
    private readonly Dictionary<EntityId, (MeteorView View, Godot.Vector2 Ground, CastBurst Blast)> _meteors = new();

    /// <summary>Lado (pixels) do quadro de impacto.</summary>
    private const float ImpactSizePx = 40f;

    /// <summary>Raio (tiles) do anel de impacto com brilho (T-434), antes da escala.</summary>
    private const float ImpactRingTiles = 1.2f;

    /// <summary>Lado (pixels) do quadro do projétil.</summary>
    private const float ProjectileSizePx = 26f;
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

    /// <summary>Unidade sob o mouse na lista de batalha (T-312): anel de destaque no mapa.</summary>
    public EntityId? HighlightedEntity { get; set; }

    /// <summary>Definições (nomes e tags de status); vazio = só siglas cruas.</summary>
    public void SetContent(ContentCatalog content) => _content = content;

    /// <summary>Sprites por definição (T-053); vazio = círculos.</summary>
    public void SetSprites(SpriteCatalog sprites) => _sprites = sprites;

    /// <summary>Efeitos do pacote (T-054); vazio = só formas desenhadas.</summary>
    public void SetEffects(EffectCatalog effects) => _effects = effects;

    /// <summary>Atualiza os nós a partir da autoridade. Chamar uma vez por frame, depois de <c>Advance</c>.</summary>
    public void Render(IGameAuthority authority, IReadOnlyList<GameEvent> events)
    {
        var latest = authority.Latest;
        _flights.Clear();
        _aims.Clear();
        _shotsThisFrame.Clear();
        foreach (var e in events)
        {
            switch (e)
            {
                case AbilityStarted started:
                    // T-434: ponto mirado (Praga) e direção da linha (Flecha Perfurante); instantâneas executam no mesmo tick.
                    _aims[(started.Source, started.AbilityId)] = (started.AimPoint, started.Target);
                    break;
                case EntityRemoved removed:
                    RemoveView(removed.Entity);
                    break;
                case DamageDealt hit when hit.Amount > 0:
                    ShowText(hit.Target, DamageText(hit), Hud.HudTheme.School(hit.School));
                    Shot(hit, authority.ControlledEntity);
                    Impact(hit, authority.ControlledEntity);
                    if (_effects.HitBurst(hit.SourceId) is { } hitBurst && _views.TryGetValue(hit.Target, out var burstTarget)
                        && (!EffectSettings.Reduced || hit.Source == authority.ControlledEntity || hit.Target == authority.ControlledEntity))
                    {
                        // T-444: Combustão (a explosão em cada monstro que queimava) e Sifão (o impacto no alvo).
                        AddChild(BurstView.Create(burstTarget.Position, hitBurst));
                    }

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
                case AbilityExecuted used:
                    if (_views.TryGetValue(used.Source, out var userView))
                    {
                        userView.PlayAttack();
                    }

                    Projectiles(used.Source, used.Targets, used.AbilityId, authority.ControlledEntity);
                    Bolt(latest, used.Source, used.Targets, used.AbilityId, authority.ControlledEntity);
                    Cone(latest, used.Source, used.Targets, used.AbilityId);
                    if (_effects.Burst(used.AbilityId) is { AtAim: true } aimBurst)
                    {
                        AimBurst(latest, used, aimBurst);
                    }
                    else if (_effects.Burst(used.AbilityId) is { } burst && userView is not null)
                    {
                        // Raio do NH de quem usou (o Bastião passa de 5 para 7 no NH3); sem alcance nos dados, o do effects.json.
                        var rank = latest?.Entities.FirstOrDefault(s => s.Id == used.Source)?.TryGet<AbilityState>()?.RankOf(used.AbilityId) ?? 1;
                        var reach = _content.AbilityAt(used.AbilityId, rank)?.Effects.OfType<ExtraTargetsEffect>()
                            .Select(x => (float)x.Radius).DefaultIfEmpty(burst.Radius).Max() ?? burst.Radius;
                        // T-424: com "at": "target" (Golpe Sagrado), a onda sai do alvo principal, até o raio dos alvos extras.
                        var center = burst.AtTarget
                            ? used.Targets.Select(t => _views.GetValueOrDefault(t)).FirstOrDefault(v => v is not null)?.Position
                            : userView.Position;
                        if (center is { } at)
                        {
                            AddChild(BurstView.Create(at, burst with { Radius = reach }));
                        }
                    }

                    if (_effects.Pillar(used.AbilityId) is { } pillar)
                    {
                        // T-424: coluna de luz caindo em cada alvo (Julgamento).
                        foreach (var target in used.Targets)
                        {
                            if (_views.TryGetValue(target, out var targetView))
                            {
                                AddChild(PillarView.Create(targetView.Position, targetView.HeadPx, pillar));
                            }
                        }
                    }

                    Pierce(latest, used, authority.ControlledEntity);
                    if (_effects.Spread(used.AbilityId) is { } spreadColor)
                    {
                        SpreadLines(latest, used, events, spreadColor);
                    }

                    if (MultiHit(latest, used.Source, used.AbilityId) is { } first)
                    {
                        // T-064: habilidade com vários acertos — "×3" uma vez e o rastro de cada acerto em alvo único.
                        ShowText(used.Source, $"{first.Name} ×{first.HitCount}", HitCountColor);
                        ShotTrails(used.Source, used.Targets, first);
                    }

                    break;
                case AbilityHit extra:
                    if (_content.Abilities.TryGetValue(extra.AbilityId, out var extraDef))
                    {
                        ShotTrails(extra.Source, extra.Targets, extraDef);
                    }

                    Projectiles(extra.Source, extra.Targets, extra.AbilityId, authority.ControlledEntity);

                    break;
                case Game.Core.Combat.Effects.ChainJumped jump
                    when _views.TryGetValue(jump.From, out var fromView) && _views.TryGetValue(jump.To, out var toView):
                    AddChild(TrailView.Create(fromView.Position, toView.Position, 7f, ChainColor, lifetime: 0.8));
                    break;
                case StatusExpired expired when _effects.Jump(expired.StatusId) is { } jumpColor:
                    JumpLine(expired, events, jumpColor);
                    break;
                case EntityRevived revived:
                    ShowText(revived.Entity, "Levantado!", Hud.HudTheme.Heal);
                    break;
                case HealApplied heal when heal.Effective > 0:
                    ShowText(heal.Target, $"+{heal.Effective:0}", Hud.HudTheme.Heal);
                    break;
                case ItemRejected itemRejected when itemRejected.Entity == authority.ControlledEntity:
                    ShowText(itemRejected.Entity, Hud.ItemText.Rejection(itemRejected.Reason), RejectionColor);
                    break;
                case GoldGained gold when gold.Entity == authority.ControlledEntity && gold.Amount > 0:
                    ShowText(gold.Entity, $"+{gold.Amount} de ouro", Hud.HudTheme.Gold);
                    break;
                case ItemPickedUp picked when picked.Entity == authority.ControlledEntity:
                    var pickedName = _content.Items.TryGetValue(picked.BaseId, out var pickedDef) ? pickedDef.Name : picked.BaseId;
                    var rarity = latest?.Entities.FirstOrDefault(s => s.Id == picked.Entity)?.TryGet<Inventory>()?[picked.InventorySlot]?.Rarity;
                    ShowText(picked.Entity, "+ " + pickedName, rarity is { } r ? Hud.ItemText.RarityColor(r) : Colors.White);
                    break;
                case AbilityRejected rejected when rejected.Source == authority.ControlledEntity:
                    var pool = latest?.Entities.FirstOrDefault(s => s.Id == rejected.Source)?.TryGet<ResourcePool>();
                    ShowRejection(rejected.Source, RejectionText.For(rejected.Reason, pool?.Type));
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
        _links.Clear();
        _beams.Clear();
        RenderGroundItems(latest, authority.ControlledEntity, seen);
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
                if (_effects.Meteor(telegraph.AbilityId) is { } meteor)
                {
                    // T-444, Meteoro: a pedra cai durante o aviso; a explosão sai quando o aviso some (o impacto).
                    if (!_meteors.TryGetValue(state.Id, out var falling))
                    {
                        falling = (MeteorView.Create(meteor.Color), default, meteor with { Radius = telegraph.Radius });
                        AddChild(falling.View);
                    }

                    var ground = Units.ToPixels(transform.Position);
                    falling.View.SetProgress(ground, (float)((now - telegraph.StartTick) / span));
                    _meteors[state.Id] = falling with { Ground = ground };
                }

                continue;
            }

            if (state.TryGet<AreaState>() is { } area)
            {
                seen.Add(state.Id);
                var areaView = GetOrCreateGround<AreaView>(state.Id);
                areaView.SetArt(_effects.Area(area.AreaId), (int)state.Id.Value);
                areaView.Apply(area, Units.ToPixels(transform.Position), now, area.OwnerFaction != playerFaction);
                continue;
            }

            if (state.TryGet<NpcInfo>() is { } npc)
            {
                // T-323: NPC da cidade (sem vida): sprite parado, nome e, com loja, o anel do mouse.
                seen.Add(state.Id);
                var at = Units.ToPixels(transform.Position);
                _npcs[state.Id] = (at, npc);
                GetOrCreateGround<NpcView>(state.Id).Apply(npc, at, _sprites, NpcUnder(GetGlobalMousePosition()) == state.Id);
                continue;
            }

            if (state.TryGet<Health>() is null || state.TryGet<GroundItem>() is not null)
            {
                continue;
            }

            seen.Add(state.Id);
            var view = GetOrCreateView(state.Id);
            var controlled = authority.ControlledEntity == state.Id;
            view.ApplyState(state, controlled, SelectedEntity == state.Id, _sprites, HighlightedEntity == state.Id);
            view.ApplyShield(_effects.Shield, state.TryGet<ShieldState>() is { Total: > 0 });
            var (chips, marks) = StatusVisuals.From(state.TryGet<StatusEffects>(), _content, now);
            view.ApplyStatus(chips, marks);
            var auras = StatusVisuals.Auras(state.TryGet<StatusEffects>(), _effects, now, _content);
            view.ApplyAuras(auras.Select(a => a.Aura).ToList());
            foreach (var (aura, protector) in auras)
            {
                if (aura.Kind == AuraKind.Link && !protector.IsNone && protector != state.Id)
                {
                    _links.Add((state.Id, protector, aura.Color));
                }
            }
            view.SetVanishWarning(state.TryGet<AbilityState>()?.Cast is { } cast
                && _content.Abilities.TryGetValue(cast.AbilityId, out var castDef) && castDef.Tags.Contains("ai.vanish"));
            if (state.TryGet<AbilityState>() is { Cast: { Target: { } beamTarget } beamCast } beamAbilities
                && _effects.Beam(beamCast.AbilityId) is { } beam)
            {
                // T-444: raio da canalização até o alvo; a força é a parte dos pulsos que já saiu (o Raio sobe a cada pulso).
                var channel = _content.AbilityAt(beamCast.AbilityId, beamAbilities.RankOf(beamCast.AbilityId))?.Channel;
                var pulses = channel is { Pulses: > 0, IntervalTicks: > 0 }
                    ? Math.Clamp(Math.Floor((now - beamCast.StartTick) / channel.IntervalTicks) / channel.Pulses, 0, 1)
                    : 0;
                _beams.Add((state.Id, beamTarget, beam, (float)pulses));
            }

            view.SetCast(SelectedEntity == state.Id && state.TryGet<AbilityState>()?.Cast is { } selectedCast
                && selectedCast.EndTick > selectedCast.StartTick
                ? ChannelAware(selectedCast, (float)((now - selectedCast.StartTick) / (selectedCast.EndTick - selectedCast.StartTick)))
                : null);

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

        if (_linkView is null)
        {
            _linkView = new LinkView { Name = "Links" };
            AddChild(_linkView);
        }

        _linkView.Begin();
        foreach (var (target, protector, color) in _links)
        {
            if (_views.TryGetValue(target, out var a) && _views.TryGetValue(protector, out var b) && a.IsAlive && b.IsAlive)
            {
                _linkView.Add(a.Position, b.Position, color);
            }
        }

        if (_beamView is null)
        {
            _beamView = new BeamView { Name = "Beams" };
            AddChild(_beamView);
        }

        _beamView.Begin();
        foreach (var (source, target, beam, strength) in _beams)
        {
            if (_views.TryGetValue(source, out var a) && _views.TryGetValue(target, out var b) && a.IsAlive && b.IsAlive)
            {
                _beamView.Add(BodyCenter(a), BodyCenter(b), beam, strength);
            }
        }

        foreach (var id in _meteors.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _meteors.Remove(id, out var landed);
            landed.View.QueueFree();
            AddChild(BurstView.Create(landed.Ground, landed.Blast));
        }

        foreach (var id in _ground.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _ground.Remove(id, out var node);
            _items.Remove(id);
            _npcs.Remove(id);
            node!.QueueFree();
        }
    }

    /// <summary>
    /// Itens no chão (T-222). A queda deixa todos no ponto do monstro (T-204); aqui eles são espalhados em volta dele,
    /// em ordem de id, só no desenho. Forte e com "[F]" quando o jogador tem direito e está ao alcance de pegar. O direito
    /// segue a regra do Core no tick do snapshot (T-394): só o dono no começo, depois todos; item largado, todos.
    /// </summary>
    private void RenderGroundItems(WorldSnapshot latest, EntityId? controlled, HashSet<EntityId> seen)
    {
        var me = controlled is { } c ? latest.Entities.FirstOrDefault(e => e.Id == c) : null;
        var myPos = me?.TryGet<Transform>()?.Position;
        var range = _content.Rules.Items.PickupRange;
        var mouse = GetGlobalMousePosition();
        var groups = latest.Entities
            .Where(e => e.TryGet<GroundItem>() is not null && e.TryGet<Transform>() is not null)
            .GroupBy(e => (MathF.Round(e.Get<Transform>().Position.X, 1), MathF.Round(e.Get<Transform>().Position.Y, 1)));
        foreach (var group in groups)
        {
            var items = group.OrderBy(e => e.Id.Value).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var state = items[i];
                var item = state.Get<GroundItem>();
                var at = state.Get<Transform>().Position;
                var drawn = items.Count == 1 ? at
                    : at + (ItemSpread * new System.Numerics.Vector2(MathF.Cos(i * MathF.Tau / items.Count), MathF.Sin(i * MathF.Tau / items.Count)));
                var mine = controlled is { } id && item.CanPickUp(id, latest.Tick);
                var inReach = myPos is { } p && System.Numerics.Vector2.Distance(p, at) <= range;
                seen.Add(state.Id);
                _items[state.Id] = (Units.ToPixels(drawn), mine, inReach);
            }
        }

        // Nome só no item sob o mouse e no que o F pegaria (vários itens juntos não embaralham os nomes).
        var next = myPos is { } at0
            ? _items.Where(kv => seen.Contains(kv.Key) && kv.Value.Mine && kv.Value.InReach)
                .OrderBy(kv => kv.Value.Pixels.DistanceTo(Units.ToPixels(at0)))
                .Select(kv => (EntityId?)kv.Key)
                .FirstOrDefault()
            : null;
        foreach (var state in latest.Entities)
        {
            if (state.TryGet<GroundItem>() is { } item && _items.TryGetValue(state.Id, out var drawnItem) && seen.Contains(state.Id))
            {
                GetOrCreateGround<GroundItemView>(state.Id).Apply(item, drawnItem.Pixels, _content, drawnItem.Mine, next == state.Id,
                    drawnItem.Pixels.DistanceTo(mouse) <= GroundItemView.RadiusPx + PickMarginPx);
            }
        }
    }

    /// <summary>NPC sob o ponto (pixels), o mais próximo; null se nenhum (T-323). O alvo do clique é o corpo, não só os pés.</summary>
    public (EntityId Id, NpcInfo Info)? PickNpcAt(Godot.Vector2 pixels) =>
        NpcUnder(pixels) is { } id ? (id, _npcs[id].Info) : null;

    private EntityId? NpcUnder(Godot.Vector2 pixels) =>
        _npcs
            .Where(kv => (kv.Value.Pixels + new Godot.Vector2(0, -NpcView.PickRadiusPx)).DistanceTo(pixels) <= NpcView.PickRadiusPx + PickMarginPx)
            .OrderBy(kv => kv.Value.Pixels.DistanceTo(pixels))
            .Select(kv => (EntityId?)kv.Key)
            .FirstOrDefault();

    /// <summary>Item no chão sob o ponto (pixels), o mais próximo; null se nenhum (T-222).</summary>
    public EntityId? PickGroundItemAt(Godot.Vector2 pixels) =>
        _items
            .Where(kv => kv.Value.Pixels.DistanceTo(pixels) <= GroundItemView.RadiusPx + PickMarginPx)
            .OrderBy(kv => kv.Value.Pixels.DistanceTo(pixels))
            .Select(kv => (EntityId?)kv.Key)
            .FirstOrDefault();

    /// <summary>Item no chão do jogador mais perto dele que está ao alcance (tecla F, T-222); null se nenhum.</summary>
    public EntityId? NearestPickableItem(EntityId? controlled)
    {
        if (controlled is not { } me || !TryGetDrawnPosition(me, out var at))
        {
            return null;
        }

        return _items
            .Where(kv => kv.Value.Mine && kv.Value.InReach)
            .OrderBy(kv => kv.Value.Pixels.DistanceTo(at))
            .Select(kv => (EntityId?)kv.Key)
            .FirstOrDefault();
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

    /// <summary>
    /// Projétil desenhado (T-054) de quem usou até cada alvo. Com efeitos reduzidos, só os que saem do jogador ou
    /// vão para ele.
    /// </summary>
    private void Projectiles(EntityId source, IReadOnlyList<EntityId> targets, string abilityId, EntityId? me)
    {
        if (_effects.Projectile(abilityId) is not { } anim || !_views.TryGetValue(source, out var from))
        {
            return;
        }

        foreach (var t in targets)
        {
            if (t == source || !_views.TryGetValue(t, out var to) || (EffectSettings.Reduced && source != me && t != me))
            {
                continue;
            }

            var a = BodyCenter(from);
            var b = BodyCenter(to);
            var style = _effects.ProjectileStyle(abilityId);
            if (style.Glow is { } glow)
            {
                // T-434 (Tiro Certeiro): rastro dourado do arco ao alvo, por baixo da flecha maior.
                AddChild(TrailView.Create(a, b, 6f * style.Scale, glow, lifetime: 0.35));
            }

            AddChild(EffectView.Projectile(anim, a, b, ProjectileSizePx * style.Scale, glow: style.Glow));
            _flights[(source, t)] = EffectView.FlightSeconds(a, b);
            if (style.OnArrive is { } arrive)
            {
                // T-434 (Ferida Aberta): sem dano na hora, o impacto sai quando a flecha chega.
                AddChild(EffectView.Impact(arrive, b, ImpactSizePx, _flights[(source, t)]));
            }
        }
    }

    /// <summary>
    /// T-434 (Praga): onda que sai do ponto mirado até o raio da área da habilidade no NH de quem usou. Sem ponto mirado,
    /// sai do primeiro alvo.
    /// </summary>
    private void AimBurst(WorldSnapshot? latest, AbilityExecuted used, CastBurst burst)
    {
        Godot.Vector2? at = _aims.TryGetValue((used.Source, used.AbilityId), out var aim) && aim.Aim is { } point
            ? Units.ToPixels(point)
            : used.Targets.Count > 0 && _views.TryGetValue(used.Targets[0], out var first) ? first.Position : null;
        if (at is not { } center)
        {
            return;
        }

        var rank = latest?.Entities.FirstOrDefault(x => x.Id == used.Source)?.TryGet<AbilityState>()?.RankOf(used.AbilityId) ?? 1;
        var radius = _content.AbilityAt(used.AbilityId, rank)?.Targeting is CircleAtPointTargeting circle ? (float)circle.Radius : burst.Radius;
        AddChild(BurstView.Create(center, burst with { Radius = radius }));
    }

    /// <summary>
    /// T-434 (Flecha Perfurante): a flecha vai de quem usou até o fim da linha, na direção mirada (ou do alvo, ou para
    /// onde olha), passando pelos alvos sem parar. O comprimento é o da linha no NH de quem usou.
    /// </summary>
    private void Pierce(WorldSnapshot? latest, AbilityExecuted used, EntityId? me)
    {
        if (_effects.Pierce(used.AbilityId) is not { } pierce || !_views.TryGetValue(used.Source, out var from)
            || (EffectSettings.Reduced && used.Source != me))
        {
            return;
        }

        var self = latest?.Entities.FirstOrDefault(x => x.Id == used.Source);
        var rank = self?.TryGet<AbilityState>()?.RankOf(used.AbilityId) ?? 1;
        if (_content.AbilityAt(used.AbilityId, rank)?.Targeting is not LineTargeting line || self?.TryGet<Transform>() is not { } tr)
        {
            return;
        }

        var dir = tr.Facing;
        if (_aims.TryGetValue((used.Source, used.AbilityId), out var aim))
        {
            var toward = aim.Aim ?? (aim.Target is { } t ? latest?.Entities.FirstOrDefault(x => x.Id == t)?.TryGet<Transform>()?.Position : null);
            if (toward is { } p && System.Numerics.Vector2.DistanceSquared(p, tr.Position) > 0.01f)
            {
                dir = p - tr.Position;
            }
        }

        if (dir.LengthSquared() < 1e-6f)
        {
            return;
        }

        dir = System.Numerics.Vector2.Normalize(dir);
        var a = BodyCenter(from);
        var b = a + (Units.ToPixels(tr.Position + (dir * (float)line.Length)) - Units.ToPixels(tr.Position));
        if (pierce.Style.Glow is { } glow)
        {
            AddChild(TrailView.Create(a, b, 5f * pierce.Style.Scale, glow, lifetime: 0.4));
        }

        AddChild(EffectView.Projectile(pierce.Animation, a, b, ProjectileSizePx * pierce.Style.Scale, glow: pierce.Style.Glow));
        foreach (var t in used.Targets)
        {
            if (_views.TryGetValue(t, out var hitView))
            {
                // O impacto de cada alvo espera a flecha passar por ele.
                _flights[(used.Source, t)] = EffectView.FlightSeconds(a, BodyCenter(hitView));
            }
        }
    }

    /// <summary>
    /// T-434 (Flecha Dispersa): linha do alvo até cada monstro que recebeu a cópia dos status neste lote (status com as
    /// etiquetas do <c>copy_status</c> aplicados, acumulados ou renovados por quem usou em outro alvo).
    /// </summary>
    private void SpreadLines(WorldSnapshot? latest, AbilityExecuted used, IReadOnlyList<GameEvent> events, Color color)
    {
        if (used.Targets.Count == 0 || !_views.TryGetValue(used.Targets[0], out var origin))
        {
            return;
        }

        var rank = latest?.Entities.FirstOrDefault(x => x.Id == used.Source)?.TryGet<AbilityState>()?.RankOf(used.AbilityId) ?? 1;
        var tags = _content.AbilityAt(used.AbilityId, rank)?.Effects.OfType<CopyStatusEffect>().SelectMany(c => c.Tags).ToHashSet()
            ?? [];
        var copied = new HashSet<EntityId>();
        foreach (var e in events)
        {
            var (source, target, statusId) = e switch
            {
                StatusApplied x => (x.Source, x.Target, x.StatusId),
                StatusStacked x => (x.Source, x.Target, x.StatusId),
                StatusRefreshed x => (x.Source, x.Target, x.StatusId),
                _ => (EntityId.None, EntityId.None, ""),
            };
            if (source == used.Source && target != used.Targets[0] && !target.IsNone
                && _content.Statuses.TryGetValue(statusId, out var def) && def.Tags.Any(tags.Contains) && copied.Add(target)
                && _views.TryGetValue(target, out var to))
            {
                AddChild(TrailView.Create(BodyCenter(origin), BodyCenter(to), 8f, color, lifetime: 0.9));
                AddChild(BurstView.Create(to.Position, new CastBurst(color, 1f)));
            }
        }
    }

    /// <summary>T-434 (Praga): linha de quem perdeu o status que acabou até quem o recebeu no mesmo lote (o pulo).</summary>
    private void JumpLine(StatusExpired expired, IReadOnlyList<GameEvent> events, Color color)
    {
        if (!_views.TryGetValue(expired.Entity, out var from))
        {
            return;
        }

        foreach (var e in events)
        {
            if (e is StatusApplied applied && applied.StatusId == expired.StatusId && applied.Target != expired.Entity
                && _views.TryGetValue(applied.Target, out var to))
            {
                AddChild(TrailView.Create(BodyCenter(from), BodyCenter(to), 7f, color, lifetime: 0.9));
                AddChild(BurstView.Create(to.Position, new CastBurst(color, 1.2f)));
                ShowText(applied.Target, "Praga", color); // no meio de vários monstros, a linha curta some; o nome não
                return;
            }
        }
    }

    /// <summary>
    /// Projétil desenhado ao usar a habilidade (T-444, Bola de Fogo): de quem usou até o primeiro alvo, e os impactos de
    /// todos esperam a chegada. Com explosão, um clarão no primeiro alvo no raio da explosão do NH de quem usou (o ponto da
    /// explosão não vai pela rede; o primeiro alvo é o centro). Sem alvo, a bola voa o comprimento da linha e se apaga.
    /// </summary>
    private void Bolt(WorldSnapshot? latest, EntityId source, IReadOnlyList<EntityId> targets, string abilityId, EntityId? me)
    {
        if (_effects.Bolt(abilityId) is not { } bolt || !_views.TryGetValue(source, out var from)
            || (EffectSettings.Reduced && source != me && !targets.Contains(me ?? EntityId.None)))
        {
            return;
        }

        var start = BodyCenter(from);
        var state = latest?.Entities.FirstOrDefault(s => s.Id == source);
        var rank = state?.TryGet<AbilityState>()?.RankOf(abilityId) ?? 1;
        var line = _content.AbilityAt(abilityId, rank)?.Targeting as LineTargeting;
        var first = targets.Where(t => t != source).Select(t => _views.GetValueOrDefault(t)).FirstOrDefault(v => v is not null);
        if (first is null)
        {
            var facing = state?.TryGet<Transform>()?.Facing ?? System.Numerics.Vector2.UnitY;
            var end = start + new Godot.Vector2(facing.X, facing.Y).Normalized() * (float)(line?.Length ?? 8) * Units.PixelsPerUnit;
            AddChild(BoltView.Create(bolt, start, end));
            return;
        }

        var hitAt = BodyCenter(first);
        var view = BoltView.Create(bolt, start, hitAt);
        view.Target = first;
        view.TargetOffset = hitAt - first.Position;
        if (bolt.Explode)
        {
            // O clarão sai nos pés do primeiro alvo na hora da chegada (ele anda durante o voo).
            var blast = new CastBurst(bolt.Color, (float)(line?.ExplodeRadius ?? 2), Blast: true);
            var offset = view.TargetOffset;
            view.OnArrive = at => AddChild(BurstView.Create(at - offset, blast));
        }

        AddChild(view);
        var flight = BoltView.FlightSeconds(start, hitAt);
        foreach (var t in targets)
        {
            _flights[(source, t)] = flight;
        }
    }

    /// <summary>
    /// Cone de fogo ao usar uma habilidade em arco (T-444, Rajada de Chamas): na direção de quem usou, no alcance e na
    /// abertura do NH dele (o NH3 abre de 90° para 120°).
    /// </summary>
    private void Cone(WorldSnapshot? latest, EntityId source, IReadOnlyList<EntityId> targets, string abilityId)
    {
        if (_effects.Cone(abilityId) is not { } color || !_views.TryGetValue(source, out var from))
        {
            return;
        }

        var state = latest?.Entities.FirstOrDefault(s => s.Id == source);
        var rank = state?.TryGet<AbilityState>()?.RankOf(abilityId) ?? 1;
        if (_content.AbilityAt(abilityId, rank)?.Targeting is not MeleeArcTargeting arc)
        {
            return;
        }

        // Direção: para o meio dos atingidos (a mira do uso não vem no evento); sem ninguém, para onde quem usou olha.
        var hit = targets.Where(t => t != source).Select(t => _views.GetValueOrDefault(t)).OfType<EntityView>().ToList();
        var facing = state?.TryGet<Transform>()?.Facing ?? System.Numerics.Vector2.UnitY;
        var direction = hit.Count > 0
            ? hit.Aggregate(Godot.Vector2.Zero, (sum, v) => sum + v.Position) / hit.Count - from.Position
            : new Godot.Vector2(facing.X, facing.Y);
        AddChild(ConeView.Create(from.Position - new Godot.Vector2(0, from.HeadPx * 0.35f), direction,
            (float)arc.Range, (float)arc.Angle, color));
    }

    /// <summary>
    /// Disparo desenhado a cada dano de uma fonte (T-444: os orbes do Arcanista, dano com o id do status): de um ponto em
    /// volta do peito de quem atira até o alvo; o impacto espera a chegada. Os disparos do mesmo instante saem de pontos
    /// diferentes, como se cada orbe atirasse.
    /// </summary>
    private void Shot(DamageDealt hit, EntityId? me)
    {
        if (_effects.Shot(hit.SourceId) is not { } bolt || hit.Source == hit.Target
            || !_views.TryGetValue(hit.Source, out var from) || !_views.TryGetValue(hit.Target, out var to)
            || (EffectSettings.Reduced && hit.Source != me && hit.Target != me))
        {
            return;
        }

        var index = _shotsThisFrame.GetValueOrDefault(hit.Source);
        _shotsThisFrame[hit.Source] = index + 1;
        var angle = index * 2.1f + (float)Time.GetTicksMsec() / 1000f * 2.6f;
        var start = BodyCenter(from) + new Godot.Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.4f) * (from.RadiusPx * 1.4f + 8f);
        var end = BodyCenter(to);
        if (bolt.Reverse)
        {
            // T-444, Sifão: a Mana tirada volta do alvo para quem usou; o impacto no alvo não espera.
            var back = BoltView.Create(bolt, end, BodyCenter(from));
            back.Target = from;
            back.TargetOffset = BodyCenter(from) - from.Position;
            AddChild(back);
            return;
        }

        var shot = BoltView.Create(bolt, start, end);
        shot.Target = to;
        shot.TargetOffset = end - to.Position;
        AddChild(shot);
        _flights[(hit.Source, hit.Target)] = BoltView.FlightSeconds(start, end);
    }

    /// <summary>Impacto no alvo (T-054), pela fonte ou escola do dano; espera o projétil chegar, se houver.</summary>
    private void Impact(DamageDealt hit, EntityId? me)
    {
        if (_effects.Impact(hit.SourceId, hit.School) is not { } anim || !_views.TryGetValue(hit.Target, out var target)
            || (EffectSettings.Reduced && hit.Source != me && hit.Target != me))
        {
            return;
        }

        var delay = _flights.GetValueOrDefault((hit.Source, hit.Target));
        var style = _effects.ImpactStyle(hit.SourceId);
        AddChild(EffectView.Impact(anim, BodyCenter(target), ImpactSizePx * style.Scale, delay, EffectSettings.Reduced ? 0.7f : 1f));
        if (style.Glow is { } glow)
        {
            // T-434 (Tiro Certeiro): anel que se abre no chão em volta do alvo quando a flecha chega.
            AddChild(BurstView.Create(target.Position, new CastBurst(glow, ImpactRingTiles * style.Scale), delay));
        }
    }

    /// <summary>Progresso da barra fina de conjuração: a canalização (T-444) esvazia em vez de encher.</summary>
    private float ChannelAware(CastState cast, float progress) =>
        _content.Abilities.TryGetValue(cast.AbilityId, out var def) && def.Channel is not null ? 1 - progress : progress;

    /// <summary>Meio do corpo desenhado (pixels): o pivô fica nos pés.</summary>
    private static Godot.Vector2 BodyCenter(EntityView view) => view.Position + new Godot.Vector2(0, -view.HeadPx * 0.45f);

    /// <summary>
    /// Aviso de recusa sobre a unidade (ex.: "Fora de alcance"). O mesmo texto não se repete em menos de
    /// <see cref="RejectionRepeatMs"/>; vale também para avisos do próprio cliente (T-386).
    /// </summary>
    public void ShowRejection(EntityId id, string text)
    {
        var nowMs = Time.GetTicksMsec();
        if (text != _lastRejection.Text || nowMs - _lastRejection.AtMs >= RejectionRepeatMs)
        {
            _lastRejection = (text, nowMs);
            ShowText(id, text, RejectionColor);
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
        label.Position = new Godot.Vector2(side, -view.HeadPx - 18 - Math.Min(recent, 4) * 5f);
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

    /// <summary>Apaga todas as unidades e o chão (troca de zona, T-223: os ids da zona nova não têm relação com os da antiga).</summary>
    public void Clear()
    {
        foreach (var view in _views.Values)
        {
            view.QueueFree();
        }

        foreach (var node in _ground.Values)
        {
            node.QueueFree();
        }

        _views.Clear();
        _ground.Clear();
        _items.Clear();
        _npcs.Clear();
        _previousById.Clear();
        _indexedPrevious = null;
        SelectedEntity = null;
        _links.Clear();
        _linkView?.Begin();
        _beams.Clear();
        _beamView?.Begin();
        foreach (var (view, _, _) in _meteors.Values)
        {
            view.QueueFree();
        }

        _meteors.Clear();
    }

    private void RemoveView(EntityId id)
    {
        if (_views.Remove(id, out var view))
        {
            view.QueueFree();
        }
    }
}
