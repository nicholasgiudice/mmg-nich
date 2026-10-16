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
/// habilidade. Intenções são enviadas apenas quando algo muda (direção) ou num aperto de tecla.
/// </summary>
public sealed class PlayerInput
{
    /// <summary>Distância máxima (tiles) dos inimigos considerados pelo Tab.</summary>
    public const float TabTargetRange = 15f;

    private readonly IReadOnlyDictionary<string, AbilityDefinition> _abilities;
    private readonly Func<string, bool> _justPressed;
    private readonly Func<string, bool> _pressed;
    private Vec2 _lastMove;

    /// <param name="abilities">Definições (para saber o tipo de alvo); vazio = só envia mira e alvo.</param>
    /// <param name="justPressed">Ação apertada neste frame (padrão: <c>Godot.Input.IsActionJustPressed</c>).</param>
    /// <param name="pressed">Ação mantida (padrão: <c>Godot.Input.IsActionPressed</c>).</param>
    public PlayerInput(
        IReadOnlyDictionary<string, AbilityDefinition> abilities,
        Func<string, bool>? justPressed = null,
        Func<string, bool>? pressed = null)
    {
        _abilities = abilities;
        _justPressed = justPressed ?? (a => Godot.Input.IsActionJustPressed(a));
        _pressed = pressed ?? (a => Godot.Input.IsActionPressed(a));
    }

    /// <summary>Alvo selecionado (clique ou Tab); limpo quando morre ou some.</summary>
    public EntityId? SelectedTarget { get; private set; }

    /// <summary>Último ponto de mira no mundo (tiles).</summary>
    public Vec2 AimPoint { get; private set; }

    /// <summary>Lê o input do frame e envia intenções. <paramref name="hovered"/> é a unidade sob o cursor.</summary>
    public void Update(IGameAuthority authority, Vec2 aimPoint, EntityId? hovered)
    {
        AimPoint = aimPoint;
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

        if (_justPressed(InputActions.Attack))
        {
            if (hovered is { } clicked && clicked != player)
            {
                SelectedTarget = clicked;
            }

            if (abilities.BasicAttackId is { } basic)
            {
                UseAbility(authority, self, units, basic, hovered);
            }
        }

        var slots = abilities.Known.Where(a => a != abilities.BasicAttackId).ToList();
        for (var i = 0; i < InputActions.AbilitySlots.Length && i < slots.Count; i++)
        {
            if (_justPressed(InputActions.AbilitySlots[i]))
            {
                UseAbility(authority, self, units, slots[i], hovered);
            }
        }

        if (abilities.Consumables.Count > 0 && _justPressed(InputActions.Consumable))
        {
            UseAbility(authority, self, units, abilities.Consumables[0], hovered);
        }
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
    /// </summary>
    private void UseAbility(IGameAuthority authority, EntitySnapshot self, IReadOnlyDictionary<EntityId, EntitySnapshot> units,
        string abilityId, EntityId? hovered)
    {
        EntityId? target = hovered ?? SelectedTarget;
        _abilities.TryGetValue(abilityId, out var ability);
        if (ability?.Targeting is UnitTargeting unit)
        {
            bool Fits(EntityId? id) =>
                id is { } v && units.TryGetValue(v, out var s) && IsAlive(s) && MatchesFaction(self, s, unit.Faction);

            target = Fits(hovered) ? hovered
                : Fits(SelectedTarget) ? SelectedTarget
                : unit.Faction == FactionFilter.Ally ? self.Id
                : target;
        }

        authority.Send(new UseAbilityIntent(self.Id, abilityId, target, AimPoint));
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
