using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Uma unidade: sprite animado (T-053) ou, sem sprite, círculo pela facção com indicador de direção; nome,
/// barra de vida, status e seleção por cima. Só exibe o que recebe do snapshot; não altera nada na simulação.
/// </summary>
public partial class EntityView : Node2D
{
    /// <summary>Depois do último deslocamento, continua "andando" por este tempo (evita piscar entre ticks).</summary>
    private const double MovingGraceSeconds = 0.12;

    private SpriteDefinition? _sprite;
    private bool _spriteResolved;
    private bool _casting;
    private double _movingFor;
    private double _attackTime = -1;
    private double _deathTime;
    private double _animTime;

    private static readonly Color PlayerColor = new(0.30f, 0.55f, 0.95f);
    private static readonly Color ControlledColor = new(0.35f, 0.85f, 1.00f);
    private static readonly Color MonsterColor = new(0.85f, 0.30f, 0.25f);
    private static readonly Color DeadColor = new(0.45f, 0.45f, 0.45f);
    private static readonly Color BarBack = new(0.10f, 0.10f, 0.10f, 0.85f);
    private static readonly Color BarFill = new(0.35f, 0.85f, 0.35f);
    private static readonly Color SelectionColor = new(1f, 0.85f, 0.2f);

    private static readonly Color StunColor = new(1f, 0.9f, 0.3f);
    private static readonly Color RootColor = new(0.55f, 0.38f, 0.20f);
    private static readonly Color FrozenColor = new(0.65f, 0.9f, 1f);
    private static readonly Color SilenceColor = new(0.75f, 0.45f, 1f);

    private const float BarWidth = 36f;
    private const float BarHeight = 4f;
    private const float ChipWidth = 15f;
    private const float ChipHeight = 10f;
    private const double FlashSeconds = 0.12;

    private readonly Label _nameLabel = new();
    private float _radiusPx = 0.4f * Units.PixelsPerUnit;
    private Color _color = MonsterColor;
    private Godot.Vector2 _facing = Godot.Vector2.Down;
    private float _healthFraction = 1f;
    private bool _alive = true;
    private bool _selected;
    private IReadOnlyList<StatusChip> _chips = Array.Empty<StatusChip>();
    private ControlMarks _marks;
    private double _flash;
    private double _time;

    public EntityId Id { get; private set; }

    public bool IsAlive => _alive;

    public float RadiusPx => _radiusPx;

    /// <summary>Altura (pixels) do pivô ao topo: com sprite, a cabeça; sem sprite, a borda do círculo.</summary>
    public float HeadPx => _sprite?.HeadPx ?? _radiusPx;

    public static EntityView Create(EntityId id) => new() { Id = id, Name = $"Entity{id.Value}" };

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeConstantOverride("outline_size", 3);
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _nameLabel.Size = new Godot.Vector2(120, 16);
        AddChild(_nameLabel);
    }

    /// <summary>Atualiza dados lentos (vida, nome, cor) a partir do snapshot mais recente.</summary>
    public void ApplyState(EntitySnapshot state, bool controlled, bool selected, SpriteCatalog sprites)
    {
        if (!_spriteResolved)
        {
            _spriteResolved = true;
            _sprite = state.TryGet<UnitInfo>() is { } info ? sprites.For(info.DefinitionId) : null;
        }

        _casting = state.TryGet<AbilityState>()?.IsCasting ?? false;
        var radius = (state.TryGet<Collider>()?.Radius ?? 0.4f) * Units.PixelsPerUnit;
        var health = state.TryGet<Health>();
        var alive = health?.IsAlive ?? true;
        var fraction = health is { Max: > 0 } ? Math.Clamp(health.Current / health.Max, 0f, 1f) : 1f;
        var faction = state.TryGet<FactionTag>()?.Faction ?? Faction.Monster;
        var color = !alive ? DeadColor
            : faction == Faction.Player ? (controlled ? ControlledColor : PlayerColor)
            : MonsterColor;

        if (radius != _radiusPx || alive != _alive || fraction != _healthFraction || color != _color || selected != _selected)
        {
            if (alive != _alive)
            {
                _deathTime = 0;
                _attackTime = -1;
            }

            _selected = selected;
            _radiusPx = radius;
            _alive = alive;
            _healthFraction = fraction;
            _color = color;
            QueueRedraw();
        }

        var name = state.TryGet<UnitInfo>()?.Name ?? "";
        if (_nameLabel.Text != name)
        {
            _nameLabel.Text = name;
        }

        _nameLabel.Position = new Godot.Vector2(-_nameLabel.Size.X / 2, _radiusPx + 2);
        ZIndex = alive ? ZLayers.Units : ZLayers.DeadUnits;
    }

    /// <summary>Ícones de status e marcas de controle no corpo.</summary>
    public void ApplyStatus(IReadOnlyList<StatusChip> chips, ControlMarks marks)
    {
        if (!_alive)
        {
            chips = Array.Empty<StatusChip>();
            marks = ControlMarks.None;
        }

        if (marks != _marks || !chips.SequenceEqual(_chips))
        {
            _chips = chips;
            _marks = marks;
            QueueRedraw();
        }
    }

    /// <summary>Pisca em branco (recebeu dano).</summary>
    public void Flash()
    {
        _flash = FlashSeconds;
        QueueRedraw();
    }

    /// <summary>Toca a animação de ataque uma vez (habilidade executada).</summary>
    public void PlayAttack()
    {
        if (_alive)
        {
            _attackTime = 0;
        }
    }

    public override void _Process(double delta)
    {
        _time += delta;
        if (_sprite is not null)
        {
            _animTime += delta;
            _deathTime += delta;
            _movingFor = Math.Max(0, _movingFor - delta);
            if (_attackTime >= 0)
            {
                _attackTime += delta;
            }

            _flash = Math.Max(0, _flash - delta);
            QueueRedraw();
            return;
        }

        if (_flash > 0)
        {
            _flash = Math.Max(0, _flash - delta);
            QueueRedraw();
        }
        else if ((_marks & ControlMarks.Stunned) != 0)
        {
            QueueRedraw(); // estrelas girando
        }
    }

    /// <summary>Pose interpolada deste frame (pixels e direção normalizada).</summary>
    public void SetPose(Godot.Vector2 positionPx, Godot.Vector2 facing)
    {
        if (positionPx.DistanceSquaredTo(Position) > 0.01f)
        {
            _movingFor = MovingGraceSeconds;
        }

        Position = positionPx;
        if (facing.LengthSquared() > 1e-6f && !facing.IsEqualApprox(_facing))
        {
            _facing = facing.Normalized();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_selected)
        {
            DrawArc(Godot.Vector2.Zero, _radiusPx + 5, 0, Mathf.Tau, 32, SelectionColor, 2.5f);
        }

        if ((_marks & ControlMarks.Rooted) != 0)
        {
            // Raízes: espinhos marrons em volta dos pés.
            for (var i = 0; i < 10; i++)
            {
                var dir = Godot.Vector2.FromAngle(i * Mathf.Tau / 10);
                DrawLine(dir * (_radiusPx + 1), dir * (_radiusPx + 7), RootColor, 3f);
            }
        }

        if (_sprite is { } sprite)
        {
            DrawSprite(sprite);
        }
        else
        {
            var body = _flash > 0 ? _color.Lerp(Colors.White, (float)(_flash / FlashSeconds) * 0.8f) : _color;
            DrawCircle(Godot.Vector2.Zero, _radiusPx, body);
            if ((_marks & ControlMarks.Frozen) != 0)
            {
                DrawCircle(Godot.Vector2.Zero, _radiusPx + 3, new Color(FrozenColor, 0.55f));
                DrawArc(Godot.Vector2.Zero, _radiusPx + 3, 0, Mathf.Tau, 6, Colors.White, 2f);
            }

            DrawArc(Godot.Vector2.Zero, _radiusPx, 0, Mathf.Tau, 24, Colors.Black, 1.5f);
            if (_alive)
            {
                DrawLine(Godot.Vector2.Zero, _facing * (_radiusPx + 6), Colors.White, 2f);
            }
        }

        var barPos = new Godot.Vector2(-BarWidth / 2, -HeadPx - 8);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth, BarHeight)), BarBack);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth * _healthFraction, BarHeight)), BarFill);

        if ((_marks & ControlMarks.Stunned) != 0)
        {
            // Estrelas girando sobre a cabeça.
            var center = new Godot.Vector2(0, -HeadPx - 14);
            for (var i = 0; i < 3; i++)
            {
                var a = (float)_time * 4f + i * Mathf.Tau / 3;
                DrawCircle(center + new Godot.Vector2(Mathf.Cos(a) * 9f, Mathf.Sin(a) * 3f), 2.5f, StunColor);
            }
        }

        if ((_marks & ControlMarks.Silenced) != 0)
        {
            var c = new Godot.Vector2(_radiusPx + 4, -HeadPx);
            DrawArc(c, 4f, 0, Mathf.Tau, 12, SilenceColor, 1.5f);
            DrawLine(c + new Godot.Vector2(-3, 3), c + new Godot.Vector2(3, -3), SilenceColor, 1.5f);
        }

        DrawChips(barPos.Y - 3);
    }

    /// <summary>
    /// Sombra com a cor da facção sob os pés (o círculo de colisão achatado) e o quadro da animação do momento,
    /// com o pivô (pés) na posição da unidade. Prioridade: morte, ataque, conjuração, andar, parado.
    /// </summary>
    private void DrawSprite(SpriteDefinition sprite)
    {
        DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
        DrawCircle(Godot.Vector2.Zero, _radiusPx, new Color(_color, _alive ? 0.35f : 0.15f));
        if (_alive)
        {
            DrawArc(Godot.Vector2.Zero, _radiusPx, 0, Mathf.Tau, 24, new Color(_color, 0.9f), 2f);
        }

        if ((_marks & ControlMarks.Frozen) != 0)
        {
            DrawCircle(Godot.Vector2.Zero, _radiusPx + 3, new Color(FrozenColor, 0.55f));
        }

        DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);

        var direction = Math.Abs(_facing.X) > Math.Abs(_facing.Y)
            ? (_facing.X > 0 ? SpriteDirection.Right : SpriteDirection.Left)
            : (_facing.Y > 0 ? SpriteDirection.Down : SpriteDirection.Up);
        var (anim, time) = CurrentAnimation(sprite, direction);
        if (anim is null)
        {
            return;
        }

        var strip = anim.For(direction);
        var index = (int)(time * anim.Fps);
        index = anim.Loop ? index % strip.Count : Math.Min(index, strip.Count - 1);
        var scale = sprite.Scale;
        var dest = new Rect2(-strip.Pivot * scale, new Godot.Vector2(strip.Width, strip.Height) * scale);
        var flash = (float)(_flash / FlashSeconds);
        var modulate = !_alive ? new Color(0.75f, 0.75f, 0.75f, 0.85f)
            : flash > 0 ? new Color(1 + flash * 2, 1 + flash * 2, 1 + flash * 2)
            : (_marks & ControlMarks.Frozen) != 0 ? new Color(0.75f, 0.9f, 1.2f)
            : Colors.White;
        DrawTextureRectRegion(sprite.Texture, dest, strip.Frame(index), modulate);
    }

    private (SpriteAnimation? Animation, double Time) CurrentAnimation(SpriteDefinition sprite, SpriteDirection direction)
    {
        if (!_alive)
        {
            return (sprite.For("death") ?? sprite.For("idle"), _deathTime);
        }

        if (_attackTime >= 0 && sprite.For("attack") is { } attack)
        {
            if (_attackTime < attack.DurationSeconds(direction))
            {
                return (attack, _attackTime);
            }

            _attackTime = -1;
        }

        if (_casting && sprite.For("cast") is { } cast)
        {
            // Conjuração: segura o meio da animação (braços erguidos) até a habilidade sair.
            var strip = cast.For(direction);
            return (cast, Math.Min(_animTime % (strip.Count / cast.Fps), (strip.Count - 1) / cast.Fps));
        }

        return _movingFor > 0 && sprite.For("walk") is { } walk ? (walk, _animTime) : (sprite.For("idle"), 0);
    }

    private void DrawChips(float bottomY)
    {
        if (_chips.Count == 0)
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        var total = _chips.Count * (ChipWidth + 1) - 1;
        var x = -total / 2;
        var y = bottomY - ChipHeight;
        foreach (var chip in _chips)
        {
            var rect = new Rect2(x, y, ChipWidth, ChipHeight);
            DrawRect(rect, new Color(chip.Color, 0.85f));
            DrawRect(rect, Colors.Black, filled: false, width: 1f);
            DrawString(font, new Godot.Vector2(x, y + ChipHeight - 2), chip.Abbreviation, HorizontalAlignment.Center, ChipWidth, 8, Colors.Black);
            x += ChipWidth + 1;
        }
    }
}
