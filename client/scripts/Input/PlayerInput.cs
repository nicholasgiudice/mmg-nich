using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Vec2 = System.Numerics.Vector2;

namespace MmoRpgClient.Input;

/// <summary>
/// Transforma o input do jogador em intenções. Não valida nada (quem decide é a autoridade, que
/// responde com <c>AbilityRejected</c>); só escolhe alvo e ponto de mira conforme o tipo de alvo da
/// habilidade. Intenções são enviadas apenas quando algo muda (direção), num aperto de tecla ou, com a
/// tecla/botão mantido, quando a habilidade fica pronta de novo (T-026: ataque contínuo, sem spam).
/// Habilidades com área mirada (ponto ou linha) mostram a prévia enquanto a tecla está apertada e saem ao
/// soltar (T-028); a mira é limitada ao alcance.
/// </summary>
public sealed class PlayerInput
{
    /// <summary>Distância máxima (tiles) dos inimigos considerados pelo Tab.</summary>
    public const float TabTargetRange = 15f;

    /// <summary>Com a tecla mantida e nada mudou desde o último envio (ex.: fora de alcance), espera isto antes de tentar de novo.</summary>
    public const int HeldRetryTicks = 8;

    /// <summary>Intervalo mínimo entre dois envios da mesma habilidade mantida (o resultado leva um tick para voltar).</summary>
    public const int HeldMinGapTicks = 2;

    private readonly IReadOnlyDictionary<string, AbilityDefinition> _abilities;
    private readonly Func<string, bool> _justPressed;
    private readonly Func<string, bool> _pressed;
    private readonly Func<string, bool> _justReleased;
    private Vec2 _lastMove;
    private readonly Dictionary<string, (long SentTick, long ReadyAtSend)> _lastSent = new(StringComparer.Ordinal);

    /// <param name="abilities">Definições no NH do jogador (tipo de alvo, raio, cargas); vazio = só envia mira e alvo.</param>
    /// <param name="justPressed">Ação apertada neste frame (padrão: <c>Godot.Input.IsActionJustPressed</c>).</param>
    /// <param name="pressed">Ação mantida (padrão: <c>Godot.Input.IsActionPressed</c>).</param>
    /// <param name="justReleased">Ação solta neste frame (padrão: <c>Godot.Input.IsActionJustReleased</c>).</param>
    public PlayerInput(
        IReadOnlyDictionary<string, AbilityDefinition> abilities,
        Func<string, bool>? justPressed = null,
        Func<string, bool>? pressed = null,
        Func<string, bool>? justReleased = null)
    {
        _abilities = abilities;
        _justPressed = justPressed ?? (a => Godot.Input.IsActionJustPressed(a));
        _pressed = pressed ?? (a => Godot.Input.IsActionPressed(a));
        _justReleased = justReleased ?? (a => Godot.Input.IsActionJustReleased(a));
    }

    /// <summary>Habilidade de área cuja tecla está apertada (prévia no chão); null = nenhuma.</summary>
    public AbilityDefinition? PreviewAbility { get; private set; }

    /// <summary>Ponto onde a área da prévia cairia (mira limitada ao alcance), em tiles.</summary>
    public Vec2 PreviewPoint { get; private set; }

    /// <summary>Área mirada no chão: mostra prévia enquanto apertada e sai ao soltar.</summary>
    public static bool IsAimedArea(AbilityDefinition? def) => def?.Targeting is CircleAtPointTargeting or LineTargeting;

    /// <summary>Alvo selecionado (clique ou Tab); limpo quando morre ou some.</summary>
    public EntityId? SelectedTarget { get; private set; }

    /// <summary>
    /// Ataque básico automático no alvo selecionado (T-033). Ligado por padrão; Esc/limpar o alvo para de atacar.
    /// </summary>
    public bool AutoAttack { get; set; } = true;

    /// <summary>Último ponto de mira no mundo (tiles).</summary>
    public Vec2 AimPoint { get; private set; }

    /// <summary>
    /// Lê o input do frame e envia intenções. <paramref name="hovered"/> é a unidade sob o cursor;
    /// com <paramref name="pointerOverUi"/> o clique é do HUD e não vira ataque.
    /// </summary>
    public void Update(IGameAuthority authority, Vec2 aimPoint, EntityId? hovered, bool pointerOverUi = false)
    {
        AimPoint = aimPoint;
        PreviewAbility = null;
        var latest = authority.Latest;
        if (authority.ControlledEntity is not { } player || latest is null)
        {
            return;
        }

        var units = latest.Entities.Where(e => e.TryGet<Transform>() is not null && e.TryGet<Health>() is not null)
            .ToDictionary(e => e.Id);
        if (!units.TryGetValue(player, out var self))
        {
            return;
        }

        if (SelectedTarget is { } sel && (!units.TryGetValue(sel, out var selState) || !IsAlive(selState)))
        {
            SelectedTarget = null;
        }

        if (hovered is { } h && (!units.TryGetValue(h, out var hs) || !IsAlive(hs)))
        {
            hovered = null;
        }

        SendMove(authority, player);

        if (_justPressed(InputActions.TargetClear))
        {
            SelectedTarget = null;
        }

        if (_justPressed(InputActions.TargetNext))
        {
            SelectedTarget = NextEnemy(units.Values, self);
        }

        var abilities = self.TryGet<AbilityState>();
        if (abilities is null)
        {
            return;
        }

        var tick = authority.CurrentTick;
        var basicSent = false;
        if (!pointerOverUi && _justPressed(InputActions.Attack))
        {
            basicSent = true;
            if (hovered is { } clicked && clicked != player)
            {
                SelectedTarget = clicked;
            }

            if (abilities.BasicAttackId is { } basic)
            {
                UseAbility(authority, self, units, basic, hovered, abilities, tick);
            }
        }
        else if (!pointerOverUi && _pressed(InputActions.Attack) && abilities.BasicAttackId is { } held
            && CanRepeat(held, abilities, tick))
        {
            basicSent = true;
            UseAbility(authority, self, units, held, hovered, abilities, tick);
        }

        // Espaço: ataca o alvo selecionado (Tab/clique) mirando nele; o cursor só conta sem seleção.
        if (abilities.BasicAttackId is { } keyAttack
            && (_justPressed(InputActions.AttackKey) || (_pressed(InputActions.AttackKey) && CanRepeat(keyAttack, abilities, tick))))
        {
            basicSent = true;
            UseAbility(authority, self, units, keyAttack, hovered, abilities, tick, preferSelected: true);
        }

        // T-033: com um inimigo selecionado, o ataque básico sai sozinho quando está pronto e o alvo está no alcance.
        if (!basicSent && AutoAttack && abilities.BasicAttackId is { } autoBasic
            && SelectedTarget is { } autoTarget && units.TryGetValue(autoTarget, out var target)
            && IsAlive(target) && MatchesFaction(self, target, FactionFilter.Enemy)
            && InReach(autoBasic, self, target) && CanRepeat(autoBasic, abilities, tick))
        {
            UseAbility(authority, self, units, autoBasic, hovered, abilities, tick, preferSelected: true);
        }

        var slots = abilities.Known.Where(a => a != abilities.BasicAttackId).ToList();
        for (var i = 0; i < InputActions.AbilitySlots.Length && i < slots.Count; i++)
        {
            var action = InputActions.AbilitySlots[i];
            _abilities.TryGetValue(slots[i], out var def);
            if (IsAimedArea(def))
            {
                // T-028: segurar mostra onde a área vai cair; soltar conjura.
                if (_pressed(action))
                {
                    PreviewAbility = def;
                    PreviewPoint = ClampAim(def!, self.Get<Transform>().Position, AimPoint);
                }
                else if (_justReleased(action))
                {
                    UseAbility(authority, self, units, slots[i], hovered, abilities, tick);
                }
            }
            else if (_justPressed(action) || (_pressed(action) && CanRepeat(slots[i], abilities, tick)))
            {
                UseAbility(authority, self, units, slots[i], hovered, abilities, tick);
            }
        }

        // Poção só no aperto: segurar não deve gastar todas.
        if (abilities.Consumables.Count > 0 && _justPressed(InputActions.Consumable))
        {
            UseAbility(authority, self, units, abilities.Consumables[0], hovered, abilities, tick);
        }
    }

    /// <summary>
    /// Repetição com a tecla mantida: só quando a habilidade estará pronta no próximo tick (a intenção é
    /// aplicada nele), sem conjuração em andamento e sem reenviar enquanto a resposta não chega. Se a recarga
    /// não mudou desde o último envio, a autoridade recusou (alcance, alvo...): espera <see cref="HeldRetryTicks"/>.
    /// </summary>
    private bool CanRepeat(string abilityId, AbilityState abilities, long tick)
    {
        var ready = abilities.ReadyAtTick.TryGetValue(abilityId, out var t) ? t : 0;
        // T-060/T-064: com cargas por recarga, a habilidade pode estar pronta mesmo com a recarga correndo.
        var onCooldown = _abilities.TryGetValue(abilityId, out var def) ? abilities.IsOnCooldown(def, tick + 1) : ready > tick + 1;
        if (onCooldown || abilities.IsCasting)
        {
            return false;
        }

        if (!_lastSent.TryGetValue(abilityId, out var last))
        {
            return true;
        }

        var since = tick - last.SentTick;
        return since >= HeldMinGapTicks && (ready != last.ReadyAtSend || since >= HeldRetryTicks);
    }

    private void SendMove(IGameAuthority authority, EntityId player)
    {
        var x = (_pressed(InputActions.MoveRight) ? 1 : 0) - (_pressed(InputActions.MoveLeft) ? 1 : 0);
        var y = (_pressed(InputActions.MoveDown) ? 1 : 0) - (_pressed(InputActions.MoveUp) ? 1 : 0);
        var dir = new Vec2(x, y);
        if (dir == _lastMove)
        {
            return;
        }

        _lastMove = dir;
        authority.Send(dir == Vec2.Zero ? new StopIntent(player) : new MoveIntent(player, Vec2.Normalize(dir)));
    }

    /// <summary>
    /// Mira sempre vai junto (dá a direção de arcos, linhas, áreas e deslocamentos). Para alvo único:
    /// unidade sob o cursor, senão a selecionada, desde que da facção certa; cura sem aliado mirado vai em si mesmo.
    /// Com <paramref name="preferSelected"/> (ataque pelo teclado) a ordem se inverte e a mira vai no alvo selecionado.
    /// </summary>
    private void UseAbility(IGameAuthority authority, EntitySnapshot self, IReadOnlyDictionary<EntityId, EntitySnapshot> units,
        string abilityId, EntityId? hovered, AbilityState abilities, long tick, bool preferSelected = false)
    {
        _lastSent[abilityId] = (tick, abilities.ReadyAtTick.TryGetValue(abilityId, out var ready) ? ready : 0);
        var (first, second) = preferSelected ? (SelectedTarget, hovered) : (hovered, SelectedTarget);
        EntityId? target = first ?? second;
        _abilities.TryGetValue(abilityId, out var ability);
        if (ability?.Targeting is UnitTargeting unit)
        {
            bool Fits(EntityId? id) =>
                id is { } v && units.TryGetValue(v, out var s) && IsAlive(s) && MatchesFaction(self, s, unit.Faction);

            target = Fits(first) ? first
                : Fits(second) ? second
                : unit.Faction == FactionFilter.Ally ? self.Id
                : target;
        }

        var aim = preferSelected && SelectedTarget is { } sel && units.TryGetValue(sel, out var selected)
            ? selected.Get<Transform>().Position
            : AimPoint;
        if (ability is not null)
        {
            aim = ClampAim(ability, self.Get<Transform>().Position, aim);
        }
        authority.Send(new UseAbilityIntent(self.Id, abilityId, target, aim));
    }

    /// <summary>
    /// Alvo ao alcance do ataque (só para não enviar à toa; quem decide é a autoridade). Sem definição
    /// conhecida, considera que sim.
    /// </summary>
    private bool InReach(string abilityId, EntitySnapshot self, EntitySnapshot target)
    {
        if (!_abilities.TryGetValue(abilityId, out var def))
        {
            return true;
        }

        var range = def.Targeting switch
        {
            UnitTargeting u => u.Range,
            MeleeArcTargeting m => m.Range,
            _ => double.MaxValue,
        };
        var distance = Vec2.Distance(self.Get<Transform>().Position, target.Get<Transform>().Position)
            - (target.TryGet<Collider>()?.Radius ?? 0f);
        return distance <= range;
    }

    /// <summary>Área em ponto além do alcance: mira no ponto mais distante permitido na direção do cursor.</summary>
    public static Vec2 ClampAim(AbilityDefinition ability, Vec2 origin, Vec2 aim)
    {
        if (ability.Targeting is not CircleAtPointTargeting point)
        {
            return aim;
        }

        var offset = aim - origin;
        var range = (float)point.Range - 0.01f;
        return offset.Length() > range ? origin + Vec2.Normalize(offset) * range : aim;
    }

    private EntityId? NextEnemy(IEnumerable<EntitySnapshot> units, EntitySnapshot self)
    {
        var origin = self.Get<Transform>().Position;
        var enemies = units
            .Where(u => IsAlive(u) && MatchesFaction(self, u, FactionFilter.Enemy))
            .Select(u => (u.Id, Distance: Vec2.Distance(origin, u.Get<Transform>().Position)))
            .Where(u => u.Distance <= TabTargetRange)
            .OrderBy(u => u.Distance)
            .ThenBy(u => u.Id.Value)
            .Select(u => u.Id)
            .ToList();
        if (enemies.Count == 0)
        {
            return null;
        }

        var index = SelectedTarget is { } current ? enemies.IndexOf(current) : -1;
        return enemies[(index + 1) % enemies.Count];
    }

    private static bool IsAlive(EntitySnapshot s) => s.TryGet<Health>()?.IsAlive ?? true;

    private static bool MatchesFaction(EntitySnapshot self, EntitySnapshot other, FactionFilter filter)
    {
        var mine = self.TryGet<FactionTag>()?.Faction;
        var theirs = other.TryGet<FactionTag>()?.Faction;
        return filter switch
        {
            FactionFilter.Any => true,
            FactionFilter.Ally => mine is not null && mine == theirs,
            FactionFilter.Enemy => mine is not null && theirs is not null && mine != theirs,
            _ => false,
        };
    }
}
