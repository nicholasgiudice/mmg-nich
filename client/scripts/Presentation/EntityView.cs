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
    private string? _spriteSecondClass;
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
    private static readonly Color CastFill = new(0.90f, 0.35f, 0.25f);
    private static readonly Color SelectionColor = new(1f, 0.85f, 0.2f);
    private static readonly Color HighlightColor = new(1f, 1f, 1f, 0.9f);

    private static readonly Color StunColor = new(1f, 0.9f, 0.3f);
    private static readonly Color RootColor = new(0.55f, 0.38f, 0.20f);
    private static readonly Color FrozenColor = new(0.65f, 0.9f, 1f);
    private static readonly Color SilenceColor = new(0.75f, 0.45f, 1f);
    private static readonly Color FearColor = new(0.62f, 0.30f, 0.85f);
    private static readonly Color MistColor = new(0.85f, 0.92f, 1f);

    /// <summary>Marca do alvo no chão (T-388, kit pedra e ouro); sem o arquivo, volta o arco dourado.</summary>
    private const string TargetMarkPath = "res://assets/art/ui/target_mark.png";

    /// <summary>Pulso da marca do alvo: período (s) e transparência mínima.</summary>
    private const double TargetPulseSeconds = 1.6;
    private const float TargetPulseMinAlpha = 0.7f;

    /// <summary>Tamanho da marca do alvo: pelo menos este diâmetro (pixels), ou o raio do corpo vezes o fator nos grandes.</summary>
    private const float TargetMarkMinPx = 60f;
    private const float TargetMarkPerRadius = 3f;

    private static Texture2D? s_targetMark;
    private static bool s_targetMarkLoaded;

    /// <summary>Transparência da unidade na névoa (T-345): ainda se vê onde está, mas não dá para mirar.</summary>
    private const float VeiledAlpha = 0.3f;

    private const float BarWidth = 36f;
    private const float BarHeight = 4f;
    private const float CastHeight = 3f;
    private const float ChipWidth = 15f;
    private const float ChipHeight = 10f;
    private const double FlashSeconds = 0.12;

    /// <summary>Ampliação dos sinais de <see cref="AuraKind.Motes"/> (T-424), para lerem de longe.</summary>
    private const float MoteScale = 1.8f;

    private readonly Label _nameLabel = new();
    private float _radiusPx = 0.4f * Units.PixelsPerUnit;
    private Color _color = MonsterColor;
    private Godot.Vector2 _facing = Godot.Vector2.Down;
    private float _healthFraction = 1f;
    private bool _alive = true;
    private bool _selected;
    private bool _highlighted;
    private float? _castProgress;
    private IReadOnlyList<StatusChip> _chips = Array.Empty<StatusChip>();
    private IReadOnlyList<StatusAura> _auras = Array.Empty<StatusAura>();
    private ControlMarks _marks;
    private bool _vanishing;
    private double _flash;
    private double _time;

    public EntityId Id { get; private set; }

    public bool IsAlive => _alive;

    public float RadiusPx => _radiusPx;

    /// <summary>Altura (pixels) do pivô ao topo: com sprite, a cabeça; sem sprite, a borda do círculo.</summary>
    public float HeadPx => _sprite?.HeadPx ?? _radiusPx;

    public static EntityView Create(EntityId id) => new() { Id = id, Name = $"Entity{id.Value}" };

    private readonly ShieldView _shield = new() { ZAsRelative = false, ZIndex = ZLayers.Effects, Visible = false };

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeConstantOverride("outline_size", 3);
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _nameLabel.Size = new Godot.Vector2(120, 16);
        AddChild(_nameLabel);
        AddChild(_shield);
    }

    /// <summary>Bolha de escudo (T-054) enquanto a unidade tem escudo ativo; <paramref name="anim"/> null = nada.</summary>
    public void ApplyShield(EffectAnimation? anim, bool active)
    {
        _shield.Animation = anim;
        _shield.Visible = anim is not null && active && _alive;
        // Centro no corpo, metade da altura até a cabeça. A bolha ocupa ~60% do quadro do pacote: o quadro é maior
        // que a unidade para ela cobrir o corpo todo.
        _shield.Position = new Godot.Vector2(0, -HeadPx * 0.45f);
        _shield.SizePx = Math.Max(HeadPx * 1.9f, _radiusPx * 4f);
    }

    private sealed partial class ShieldView : Node2D
    {
        private double _time;

        public EffectAnimation? Animation { get; set; }

        public float SizePx { get; set; }

        public override void _Ready() => TextureFilter = TextureFilterEnum.Nearest;

        public override void _Process(double delta)
        {
            if (Visible)
            {
                _time += delta;
                QueueRedraw();
            }
        }

        public override void _Draw() => Animation?.Draw(this, Godot.Vector2.Zero, SizePx, _time, new Color(1, 1, 1, EffectSettings.ShieldAlpha));
    }

    /// <summary>Atualiza dados lentos (vida, nome, cor) a partir do snapshot mais recente.</summary>
    public void ApplyState(EntitySnapshot state, bool controlled, bool selected, SpriteCatalog sprites, bool highlighted = false)
    {
        // A segunda classe pode chegar no meio da sessão (escolha no nível 20): o sprite troca quando ela muda.
        var secondClass = state.TryGet<Game.Core.Combat.Progression.CharacterProgress>()?.SecondClass;
        if (!_spriteResolved || secondClass != _spriteSecondClass)
        {
            _spriteResolved = true;
            _spriteSecondClass = secondClass;
            _sprite = state.TryGet<UnitInfo>() is { } info ? sprites.ForCharacter(info.DefinitionId, secondClass) : null;
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

        if (radius != _radiusPx || alive != _alive || fraction != _healthFraction || color != _color || selected != _selected || highlighted != _highlighted)
        {
            if (alive != _alive)
            {
                _deathTime = 0;
                _attackTime = -1;
            }

            _selected = selected;
            _highlighted = highlighted;
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
    /// <summary>Sinais dos status no corpo (T-412): anel, labaredas ou anel do protegido. Vazio = nada.</summary>
    public void ApplyAuras(IReadOnlyList<StatusAura> auras)
    {
        _auras = _alive ? auras : Array.Empty<StatusAura>();
    }

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

    /// <summary>
    /// Aviso antes de sumir na névoa (T-345): a unidade conjura uma habilidade de sumiço (tag <c>ai.vanish</c>, o Véu de
    /// Névoa da Fada). Anéis de névoa sobem dos pés até a conjuração acabar.
    /// </summary>
    public void SetVanishWarning(bool vanishing)
    {
        vanishing &= _alive;
        if (vanishing != _vanishing)
        {
            _vanishing = vanishing;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Barra fina de conjuração sob a barra de vida (T-383): substitui a do quadro do alvo, que saiu do topo da tela.
    /// <paramref name="progress"/> de 0 a 1; null = sem barra.
    /// </summary>
    public void SetCast(float? progress)
    {
        progress = _alive && progress is { } p ? Math.Clamp(p, 0f, 1f) : null;
        if (progress != _castProgress)
        {
            _castProgress = progress;
            QueueRedraw();
        }
    }

    /// <summary>Pisca em branco (recebeu dano).</summary>
    public void Flash()
    {
        _flash = FlashSeconds;
        QueueRedraw();
    }

    /// <summary>Toca a animação de ataque uma vez (habilidade executada); andando, a caminhada tem prioridade.</summary>
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
        else if ((_marks & (ControlMarks.Stunned | ControlMarks.Feared)) != 0 || _vanishing || (_selected && TargetMark() is not null)
            || _auras.Count > 0)
        {
            QueueRedraw(); // estrelas girando, medo pulsando, névoa subindo, marca do alvo pulsando, sinais de status
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

    /// <summary>
    /// Marca do alvo (T-388): a arte no chão, aos pés, atrás do sprite, ampliada pelo inteiro que mais se aproxima do
    /// tamanho do corpo (sem borrar) e pulsando de leve. Sem a arte, o arco dourado de antes.
    /// </summary>
    private void DrawTargetMark()
    {
        if (TargetMark() is not { } mark)
        {
            DrawArc(Godot.Vector2.Zero, _radiusPx + 5, 0, Mathf.Tau, 32, SelectionColor, 2.5f);
            return;
        }

        var diameter = Math.Max(TargetMarkMinPx, TargetMarkPerRadius * _radiusPx);
        var scale = Math.Max(1, (int)MathF.Round(diameter / mark.GetWidth()));
        var size = mark.GetSize() * scale;
        var pulse = 0.5f + 0.5f * MathF.Cos((float)(_time * Mathf.Tau / TargetPulseSeconds));
        var alpha = TargetPulseMinAlpha + (1 - TargetPulseMinAlpha) * pulse;
        DrawTextureRect(mark, new Rect2((-size / 2).Floor(), size), tile: false, modulate: new Color(1, 1, 1, alpha));
    }

    private static Texture2D? TargetMark()
    {
        if (!s_targetMarkLoaded)
        {
            s_targetMarkLoaded = true;
            s_targetMark = SpriteCatalog.LoadTexture(TargetMarkPath);
        }

        return s_targetMark;
    }

    public override void _Draw()
    {
        if (_selected)
        {
            DrawTargetMark();
        }

        if (_highlighted)
        {
            // T-312: mouse sobre a linha na lista de batalha.
            DrawArc(Godot.Vector2.Zero, _radiusPx + (_selected ? 9 : 5), 0, Mathf.Tau, 32, HighlightColor, 2f);
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

        var veiled = (_marks & ControlMarks.Veiled) != 0;
        _nameLabel.Visible = !veiled;
        if (_vanishing)
        {
            DrawMistWarning();
        }

        if (!veiled)
        {
            DrawAuras(front: false);
        }

        if (_sprite is { } sprite)
        {
            DrawSprite(sprite, veiled ? VeiledAlpha : 1f);
        }
        else
        {
            var body = _flash > 0 ? _color.Lerp(Colors.White, (float)(_flash / FlashSeconds) * 0.8f) : _color;
            DrawCircle(Godot.Vector2.Zero, _radiusPx, veiled ? new Color(body, VeiledAlpha) : body);
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

        if (veiled)
        {
            return; // na névoa: sem barra de vida nem status (não é alvo)
        }

        DrawAuras(front: true);

        var barPos = new Godot.Vector2(-BarWidth / 2, -HeadPx - 8);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth, BarHeight)), BarBack);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth * _healthFraction, BarHeight)), BarFill);
        if (_castProgress is { } castProgress)
        {
            var castPos = barPos + new Godot.Vector2(0, BarHeight + 1);
            DrawRect(new Rect2(castPos, new Godot.Vector2(BarWidth, CastHeight)), BarBack);
            DrawRect(new Rect2(castPos, new Godot.Vector2(BarWidth * castProgress, CastHeight)), CastFill);
        }

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

        if ((_marks & ControlMarks.Feared) != 0)
        {
            // Acima das plaquetas de status (que ficam logo acima da barra de vida).
            DrawFear(new Godot.Vector2(0, barPos.Y - (_chips.Count > 0 ? ChipHeight + 13 : 10)));
        }

        DrawChips(barPos.Y - 3);
    }

    /// <summary>
    /// Sinais dos status (T-412). Atrás do corpo (<paramref name="front"/> false): o anel nos pés e as labaredas, que aparecem
    /// dos lados do sprite. Na frente: faíscas subindo da fúria, para o sinal não sumir atrás do sprite.
    /// </summary>
    private void DrawAuras(bool front)
    {
        var t = (float)_time;
        var motes = 0; // T-424: cada estado com sinais sobe numa fase própria, para não se sobreporem
        foreach (var aura in _auras)
        {
            switch (aura.Kind)
            {
                case AuraKind.Ring when !front:
                case AuraKind.Link when !front:
                    var pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);
                    var r = _radiusPx + (aura.Kind == AuraKind.Ring ? 5f + pulse * 3f : 3f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
                    if (aura.Kind == AuraKind.Ring)
                    {
                        DrawCircle(Godot.Vector2.Zero, r, new Color(aura.Color, 0.14f + 0.08f * pulse));
                    }

                    DrawArc(Godot.Vector2.Zero, r, 0, Mathf.Tau, 32, new Color(aura.Color, 0.65f + 0.3f * pulse),
                        aura.Kind == AuraKind.Ring ? 2.5f : 1.5f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Flames when !front:
                    // Línguas de fogo em volta do corpo, cada uma com altura e fase próprias.
                    var height = HeadPx * 1.15f;
                    var baseY = _radiusPx * 0.25f;
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
                    DrawCircle(new Godot.Vector2(0, baseY * 2), _radiusPx * 1.5f, new Color(aura.Color, 0.30f));
                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    for (var i = 0; i < 9; i++)
                    {
                        var side = i - 4;
                        var x = side * _radiusPx * 0.36f;
                        var flicker = 0.65f + 0.35f * Mathf.Sin(t * (9f + i) + i * 1.9f);
                        var h = height * (0.45f + 0.55f * (1f - Math.Abs(side) / 5f)) * flicker;
                        var w = _radiusPx * 0.34f;
                        DrawColoredPolygon(
                            [new Godot.Vector2(x - w, baseY), new Godot.Vector2(x + w, baseY), new Godot.Vector2(x, baseY - h)],
                            new Color(aura.Color, 0.9f));
                        DrawColoredPolygon(
                            [new Godot.Vector2(x - w * 0.5f, baseY), new Godot.Vector2(x + w * 0.5f, baseY), new Godot.Vector2(x, baseY - h * 0.55f)],
                            new Color(1f, 0.82f, 0.3f, 0.95f));
                    }

                    break;

                case AuraKind.Hobble:
                    // Corrente nos pés, girando devagar (lento e enfraquecido): a metade de trás atrás do corpo, a da frente
                    // por cima dos pés, para não sumir embaixo de sprites largos (lobos).
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
                    var hr = (_radiusPx * 1.5f) + 6f;
                    const int links = 12;
                    for (var i = 0; i < links; i++)
                    {
                        var from = (i * Mathf.Tau / links) + (t * 0.6f);
                        var mid = Mathf.PosMod(from + (Mathf.Tau / links * 0.31f), Mathf.Tau);
                        if (mid < Mathf.Pi != front)
                        {
                            continue;
                        }

                        var to = from + (Mathf.Tau / links * 0.62f);
                        DrawArc(Godot.Vector2.Zero, hr, from, to, 4, new Color(0.08f, 0.08f, 0.1f, 0.95f), 6f);
                        DrawArc(Godot.Vector2.Zero, hr, from, to, 4, new Color(aura.Color, 1f), 3.5f);
                    }

                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Drip when front:
                    // Gotas de sangue escorrendo do meio do corpo até o chão.
                    for (var i = 0; i < 4; i++)
                    {
                        var k = (t * 0.8f + i / 4f) % 1f;
                        var x = (i % 2 == 0 ? -1 : 1) * _radiusPx * (0.25f + 0.2f * i / 4f);
                        var y = -HeadPx * 0.55f + k * HeadPx * 0.6f;
                        var drop = new Color(aura.Color, 1f - k * 0.6f);
                        DrawCircle(new Godot.Vector2(x, y), 2.6f, drop);
                        DrawColoredPolygon([new Godot.Vector2(x - 2.2f, y - 0.5f), new Godot.Vector2(x + 2.2f, y - 0.5f), new Godot.Vector2(x, y - 5.5f)], drop);
                    }

                    break;

                case AuraKind.Eye when front:
                    DrawEye(new Godot.Vector2(-_radiusPx - 12f, -HeadPx * 0.85f), aura.Color, t);
                    break;

                case AuraKind.Stacks when front && aura.Stacks > 0:
                    DrawStacks(new Godot.Vector2(_radiusPx + 10f, -HeadPx * 0.6f), aura.Color, aura.Stacks);
                    break;

                case AuraKind.Flames when front:
                    // Faíscas subindo pela frente do corpo.
                    for (var i = 0; i < 6; i++)
                    {
                        var k = (t * 0.9f + i / 6f) % 1f;
                        var x = Mathf.Sin(i * 2.3f + t * 2f) * _radiusPx * 0.9f;
                        var y = -k * HeadPx * 1.2f;
                        DrawRect(new Rect2(x - 1.5f, y - 1.5f, 3f, 3f), new Color(1f, 0.75f, 0.25f, 1f - k));
                    }

                    break;

                case AuraKind.Motes when front:
                    // T-424: três sinais pequenos subindo dos lados do corpo, um de cada vez, sumindo no alto.
                    var phase = motes++ / 9f;
                    for (var i = 0; i < 3; i++)
                    {
                        var k = (t * 0.55f + i / 3f + phase) % 1f;
                        var side = i % 2 == 0 ? 1f : -1f;
                        var x = side * (_radiusPx + 7f) + (i == 1 ? 0 : -side * 3f);
                        var y = _radiusPx * 0.2f - k * HeadPx * 1.15f;
                        var fade = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                        DrawSetTransform(new Godot.Vector2(x, y), 0, new Godot.Vector2(MoteScale, MoteScale));
                        DrawGlyph(aura.Glyph, Godot.Vector2.Zero, aura.Color, fade);
                    }

                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Wind when !front:
                    // T-424: três arcos claros girando rápido em volta dos pés, como vento.
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.45f));
                    for (var i = 0; i < 3; i++)
                    {
                        var from = t * 7f + i * Mathf.Tau / 3;
                        var rw = _radiusPx + 7f + i * 3f;
                        DrawArc(Godot.Vector2.Zero, rw, from, from + 1.6f, 12, new Color(0.05f, 0.1f, 0.15f, 0.35f), 7f);
                        DrawArc(Godot.Vector2.Zero, rw, from + 0.3f, from + 1.6f, 12, new Color(aura.Color, 1f), 4f);
                        DrawArc(Godot.Vector2.Zero, rw, from + 1.0f, from + 1.6f, 6, Colors.White, 2f);
                    }

                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Wind when front:
                    // Riscos de velocidade passando pelos lados das pernas.
                    for (var i = 0; i < 4; i++)
                    {
                        var k = (t * 2.2f + i / 4f) % 1f;
                        var side = i % 2 == 0 ? 1f : -1f;
                        var y = -HeadPx * (0.15f + 0.12f * i);
                        var x0 = side * (_radiusPx + 4f + k * 10f);
                        DrawLine(new Godot.Vector2(x0, y), new Godot.Vector2(x0 + side * 9f, y), new Color(aura.Color, 1f - k), 2.5f);
                    }

                    break;

                case AuraKind.Orbs:
                    // T-444, Orbes Arcanos: bolinhas girando numa elipse em volta do peito. As da metade de trás da volta
                    // saem atrás do sprite, as da frente por cima, para parecer que giram em volta do corpo.
                    var center = new Godot.Vector2(0, -HeadPx * 0.45f);
                    var rx = _radiusPx * 1.4f + 8f;
                    for (var i = 0; i < aura.Count; i++)
                    {
                        var angle = t * 2.6f + i * Mathf.Tau / aura.Count;
                        if (Mathf.Sin(angle) >= 0 != front)
                        {
                            continue;
                        }

                        var at = center + new Godot.Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * rx * 0.4f);
                        DrawCircle(at, 8f, new Color(aura.Color, 0.30f));
                        DrawCircle(at, 5f, aura.Color);
                        DrawCircle(at + new Godot.Vector2(-1.5f, -1.5f), 2f, new Color(1f, 0.95f, 1f));
                    }

                    break;

                case AuraKind.Burning when front:
                    // T-444, Queimadura: três chamas pequenas no corpo, tremendo, e brasas subindo.
                    for (var i = 0; i < 3; i++)
                    {
                        var at = new Godot.Vector2((i - 1) * _radiusPx * 0.55f, -HeadPx * (0.25f + 0.25f * (i % 2)));
                        var flicker = 0.75f + 0.25f * Mathf.Sin(t * (11f + i * 2f) + i * 2.1f);
                        var h = 11f * flicker;
                        DrawColoredPolygon(
                            [at + new Godot.Vector2(-4.5f, 0), at + new Godot.Vector2(4.5f, 0), at + new Godot.Vector2(0, -h)],
                            new Color(aura.Color, 0.95f));
                        DrawColoredPolygon(
                            [at + new Godot.Vector2(-2.2f, 0), at + new Godot.Vector2(2.2f, 0), at + new Godot.Vector2(0, -h * 0.55f)],
                            new Color(1f, 0.9f, 0.35f));
                    }

                    for (var i = 0; i < 3; i++)
                    {
                        var k = (t * 0.8f + i / 3f) % 1f;
                        var x = Mathf.Sin(i * 2.7f + t * 1.5f) * _radiusPx * 0.6f;
                        DrawRect(new Rect2(x - 1f, -HeadPx * (0.4f + k * 0.8f) - 1f, 2.5f, 2.5f), new Color(1f, 0.6f, 0.2f, 1f - k));
                    }

                    break;

                case AuraKind.Sigil when !front:
                    // T-424: selo dourado girando nos pés (marca), do tamanho do corpo.
                    var rs = _radiusPx + 6f;
                    var spin = t * 1.6f;
                    var glow = 0.75f + 0.25f * Mathf.Sin(t * 5f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
                    DrawCircle(Godot.Vector2.Zero, rs, new Color(aura.Color, 0.16f));
                    DrawArc(Godot.Vector2.Zero, rs, 0, Mathf.Tau, 40, new Color(aura.Color, glow), 2.5f);
                    DrawArc(Godot.Vector2.Zero, rs * 0.7f, 0, Mathf.Tau, 32, new Color(aura.Color, 0.6f * glow), 1.5f);
                    for (var i = 0; i < 4; i++)
                    {
                        var dir = Godot.Vector2.FromAngle(spin + i * Mathf.Tau / 4);
                        DrawLine(dir * rs * 0.7f, dir * rs * 1.18f, new Color(aura.Color, glow), 3f);
                    }

                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Dome when front:
                    // T-424: cúpula translúcida sobre o corpo, com a borda clara pulsando.
                    var pd = 0.5f + 0.5f * Mathf.Sin(t * 3.5f);
                    var domeRx = _radiusPx + 9f;
                    var ry = HeadPx + 8f;
                    var dome = new List<Godot.Vector2>();
                    for (var i = 0; i <= 24; i++)
                    {
                        var ang = Mathf.Pi + i * Mathf.Pi / 24;
                        dome.Add(new Godot.Vector2(Mathf.Cos(ang) * domeRx, _radiusPx * 0.3f + Mathf.Sin(ang) * ry));
                    }

                    DrawColoredPolygon([.. dome], new Color(aura.Color, 0.14f + 0.06f * pd));
                    DrawPolyline([.. dome], new Color(aura.Color, 0.7f + 0.3f * pd), 2f);
                    DrawSetTransform(new Godot.Vector2(0, _radiusPx * 0.3f), 0, new Godot.Vector2(1, 0.4f));
                    DrawArc(Godot.Vector2.Zero, domeRx, 0, Mathf.Tau, 32, new Color(aura.Color, 0.6f + 0.3f * pd), 2f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Mark when front:
                    // T-444, Marca Arcana: losango roxo à direita da barra de vida, com o número de pilhas.
                    var c = new Godot.Vector2(BarWidth / 2 + 9, -HeadPx - 6);
                    const float d = 8f;
                    DrawColoredPolygon(
                        [c + new Godot.Vector2(0, -d), c + new Godot.Vector2(d, 0), c + new Godot.Vector2(0, d), c + new Godot.Vector2(-d, 0)],
                        aura.Color);
                    DrawPolyline(
                        [c + new Godot.Vector2(0, -d), c + new Godot.Vector2(d, 0), c + new Godot.Vector2(0, d), c + new Godot.Vector2(-d, 0),
                            c + new Godot.Vector2(0, -d)],
                        new Color(1f, 0.9f, 1f), 1.5f);
                    DrawString(ThemeDB.FallbackFont, c + new Godot.Vector2(-d, 4.5f), aura.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        HorizontalAlignment.Center, d * 2, 12, Colors.White);
                    break;

                case AuraKind.Mana when !front:
                    var manaPulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
                    DrawCircle(Godot.Vector2.Zero, _radiusPx + 6f, new Color(aura.Color, 0.16f + 0.08f * manaPulse));
                    DrawArc(Godot.Vector2.Zero, _radiusPx + 6f, 0, Mathf.Tau, 32, new Color(aura.Color, 0.7f + 0.3f * manaPulse), 2.5f);
                    DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
                    break;

                case AuraKind.Mana when front:
                    // T-444, Sifão Arcano: gotas azuis subindo enquanto a Mana regenera mais rápido.
                    for (var i = 0; i < 5; i++)
                    {
                        var k = (t * 0.7f + i / 5f) % 1f;
                        var x = Mathf.Sin(i * 1.9f) * _radiusPx * 0.9f;
                        var at = new Godot.Vector2(x, -k * HeadPx * 1.1f);
                        DrawCircle(at, 2.5f, new Color(aura.Color, 1f - k));
                        DrawCircle(at + new Godot.Vector2(-0.6f, -0.6f), 1f, new Color(1, 1, 1, 1f - k));
                    }

                    break;
            }
        }
    }

    /// <summary>Um sinal de <see cref="AuraKind.Motes"/> (T-424): forma cheia com contorno escuro, para ler sobre a grama.</summary>
    private void DrawGlyph(AuraGlyph glyph, Godot.Vector2 c, Color color, float alpha)
    {
        var fill = new Color(color, alpha);
        var outline = new Color(0.08f, 0.06f, 0.05f, 0.85f * alpha);
        var light = new Color(color.Lightened(0.55f), alpha);
        switch (glyph)
        {
            case AuraGlyph.Plus:
                DrawRect(new Rect2(c.X - 1.5f - 1, c.Y - 4.5f - 1, 5f, 11f), outline);
                DrawRect(new Rect2(c.X - 4.5f - 1, c.Y - 1.5f - 1, 11f, 5f), outline);
                DrawRect(new Rect2(c.X - 1.5f, c.Y - 4.5f, 3f, 9f), fill);
                DrawRect(new Rect2(c.X - 4.5f, c.Y - 1.5f, 9f, 3f), fill);
                break;

            case AuraGlyph.Drop:
                Godot.Vector2[] drop = [c + new Godot.Vector2(0, -6f), c + new Godot.Vector2(3.6f, 0.5f), c + new Godot.Vector2(-3.6f, 0.5f)];
                DrawCircle(c + new Godot.Vector2(0, 1.5f), 4.6f, outline);
                DrawColoredPolygon([c + new Godot.Vector2(0, -7.5f), c + new Godot.Vector2(4.6f, 0.5f), c + new Godot.Vector2(-4.6f, 0.5f)], outline);
                DrawCircle(c + new Godot.Vector2(0, 1.5f), 3.6f, fill);
                DrawColoredPolygon(drop, fill);
                DrawRect(new Rect2(c.X - 2f, c.Y - 0.5f, 1.5f, 2f), light);
                break;

            case AuraGlyph.Chevron:
                DrawPolyline([c + new Godot.Vector2(-4.5f, 2.5f), c + new Godot.Vector2(0, -2.5f), c + new Godot.Vector2(4.5f, 2.5f)], outline, 4.5f);
                DrawPolyline([c + new Godot.Vector2(-4.5f, 2.5f), c + new Godot.Vector2(0, -2.5f), c + new Godot.Vector2(4.5f, 2.5f)], fill, 2.5f);
                break;

            default:
                Godot.Vector2[] spark = [c + new Godot.Vector2(0, -5f), c + new Godot.Vector2(1.5f, -1.5f), c + new Godot.Vector2(5f, 0),
                    c + new Godot.Vector2(1.5f, 1.5f), c + new Godot.Vector2(0, 5f), c + new Godot.Vector2(-1.5f, 1.5f),
                    c + new Godot.Vector2(-5f, 0), c + new Godot.Vector2(-1.5f, -1.5f)];
                DrawColoredPolygon(spark, fill);
                DrawCircle(c, 1.2f, light);
                break;
        }
    }

    /// <summary>T-434, Olho de Falcão: olho dourado (amêndoa com pupila) ao lado da cabeça, com brilho pulsando.</summary>
    private void DrawEye(Godot.Vector2 c, Color color, float t)
    {
        var pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
        DrawCircle(c, 11f + pulse * 2f, new Color(color, 0.18f + 0.12f * pulse));
        const int n = 12;
        var outline = new Godot.Vector2[n * 2];
        for (var i = 0; i < n; i++)
        {
            var a = Mathf.Pi * i / (n - 1);
            outline[i] = c + new Godot.Vector2(-Mathf.Cos(a) * 9f, -Mathf.Sin(a) * 5f);
            outline[n + i] = c + new Godot.Vector2(Mathf.Cos(a) * 9f, Mathf.Sin(a) * 5f);
        }

        DrawColoredPolygon(outline, new Color(1f, 0.97f, 0.85f));
        DrawCircle(c, 4f, color);
        DrawCircle(c, 1.8f, new Color(0.1f, 0.08f, 0.05f));
        DrawPolyline([.. outline, outline[0]], new Color(0.25f, 0.15f, 0.02f), 1.5f);
    }

    /// <summary>T-434, Veneno do Caçador: gota verde com o número de acúmulos, legível de longe.</summary>
    private void DrawStacks(Godot.Vector2 c, Color color, int stacks)
    {
        var dark = new Color(0.05f, 0.12f, 0.02f);
        DrawCircle(c, 7.5f, dark);
        DrawColoredPolygon([c + new Godot.Vector2(-6.5f, -2.5f), c + new Godot.Vector2(6.5f, -2.5f), c + new Godot.Vector2(0, -13f)], dark);
        DrawCircle(c, 6f, color);
        DrawColoredPolygon([c + new Godot.Vector2(-5f, -2f), c + new Godot.Vector2(5f, -2f), c + new Godot.Vector2(0, -11f)], color);
        var font = ThemeDB.FallbackFont;
        var text = stacks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var at = c + new Godot.Vector2(-8f, 4.5f);
        DrawStringOutline(font, at, text, HorizontalAlignment.Center, 16f, 12, 3, dark);
        DrawString(font, at, text, HorizontalAlignment.Center, 16f, 12, Colors.White);
    }

    /// <summary>Medo (T-345): balão roxo com "!" sobre a cabeça, tremendo, como as estrelas do atordoamento.</summary>
    private void DrawFear(Godot.Vector2 center)
    {
        var shake = new Godot.Vector2(Mathf.Sin((float)_time * 30f) * 1.5f, 0);
        var c = center + shake;
        DrawCircle(c, 7f, FearColor);
        DrawArc(c, 7f, 0, Mathf.Tau, 16, Colors.Black, 1.5f);
        DrawLine(c + new Godot.Vector2(0, -4), c + new Godot.Vector2(0, 1), Colors.White, 2f);
        DrawCircle(c + new Godot.Vector2(0, 3.5f), 1.2f, Colors.White);
    }

    /// <summary>Aviso de sumiço na névoa: anéis claros e translúcidos que crescem a partir dos pés.</summary>
    private void DrawMistWarning()
    {
        DrawSetTransform(Godot.Vector2.Zero, 0, new Godot.Vector2(1, 0.5f));
        for (var i = 0; i < 3; i++)
        {
            var t = (float)((_time * 1.6 + i / 3.0) % 1.0);
            var r = _radiusPx * (1f + t * 1.8f);
            DrawCircle(Godot.Vector2.Zero, r, new Color(MistColor, 0.22f * (1 - t)));
            DrawArc(Godot.Vector2.Zero, r, 0, Mathf.Tau, 24, new Color(MistColor, 0.7f * (1 - t)), 2f);
        }

        DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
    }

    /// <summary>
    /// Sombra com a cor da facção sob os pés (o círculo de colisão achatado) e o quadro da animação do momento,
    /// com o pivô (pés) na posição da unidade. Prioridade: morte, ataque, conjuração, andar, parado.
    /// </summary>
    private void DrawSprite(SpriteDefinition sprite, float alpha)
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
        var color = modulate * sprite.Tint;
        DrawTextureRectRegion(sprite.Texture, dest, strip.Frame(index), new Color(color, color.A * alpha));
    }

    private (SpriteAnimation? Animation, double Time) CurrentAnimation(SpriteDefinition sprite, SpriteDirection direction)
    {
        if (!_alive)
        {
            return (sprite.For("death") ?? sprite.For("idle"), _deathTime);
        }

        if (_attackTime >= 0 && sprite.For("attack") is { } attack)
        {
            if (_attackTime >= attack.DurationSeconds(direction))
            {
                _attackTime = -1;
            }
            else if (_movingFor <= 0)
            {
                return (attack, _attackTime);
            }

            // Andando, o golpe sai sem trocar de animação: a caminhada continua e o projétil/efeito mostram o ataque.
            // Se parar no meio, o resto do golpe aparece.
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
