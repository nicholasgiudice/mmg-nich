using System.Numerics;
using Game.Core.Simulation;
using Game.Core.Spatial;

namespace MmoRpgClient.Authority;

/// <summary>Parâmetros do movimento do próprio jogador, lidos do snapshot mais novo do servidor.</summary>
public readonly record struct PredictionParams(float Speed, float SpeedMultiplier, float Radius, bool LockFacing);

/// <summary>
/// Predição do movimento do próprio jogador (T-108). Roda a mesma regra do <c>MovementSystem</c> do Core (velocidade ×
/// multiplicador × dt, colisão deslizante contra o mapa com <see cref="Collision.MoveCircle"/>) a 20 Hz, no relógio do
/// cliente, guardando a direção usada em cada tick.
/// <para><b>Reconciliação:</b> o servidor diz, com <c>IntentAck</c>, em que tick dele cada intenção passou a valer. Num
/// snapshot do tick T, a última intenção confirmada até T (sequência A, tick Ta) corresponde ao tick do cliente
/// <c>kA + (T − Ta)</c>, onde kA é o tick em que o cliente começou a aplicar A. O preditor volta para a posição do
/// servidor e reaplica as direções dos ticks seguintes. Correção pequena vira um deslocamento de desenho que some em
/// ~100 ms; grande (investida, empurrão, teleporte) é aplicada direto.</para>
/// Não depende do Godot (testado em <c>tests/Game.Client.Tests</c>).
/// </summary>
public sealed class MovementPredictor(GridMap? map)
{
    /// <summary>Correção maior que isso (tiles) é aplicada direto; menor é suavizada.</summary>
    public const float SnapDistance = 2f;

    /// <summary>Velocidade com que o deslocamento de correção some (1/s): ~95% em 0,2 s.</summary>
    public const float SmoothingRate = 15f;

    /// <summary>Ticks de histórico guardados (6,4 s); confirmação mais velha que isso força posição do servidor.</summary>
    public const int HistoryTicks = 128;

    private readonly List<(long Tick, Vector2 Dir)> _history = new();
    private readonly List<(uint Seq, long ClientTick)> _sent = new();
    private readonly List<(uint Seq, long ServerTick)> _acks = new();
    private Vector2 _position;
    private Vector2 _previous;
    private Vector2 _direction;
    private Vector2 _offset;
    private double _accumulator;

    /// <summary>Ticks previstos desde o início.</summary>
    public long Tick { get; private set; }

    /// <summary>Já tem posição (recebeu o primeiro snapshot).</summary>
    public bool Ready { get; private set; }

    /// <summary>Posição prevista no fim do tick atual (sem suavização).</summary>
    public Vector2 Position => _position;

    /// <summary>Direção de movimento atual (o que o jogador está apertando).</summary>
    public Vector2 Direction => _direction;

    /// <summary>Deslocamento de desenho que ainda está sumindo (correção suavizada).</summary>
    public Vector2 Offset => _offset;

    /// <summary>Tamanho da última correção (tiles) e se foi aplicada direto.</summary>
    public (float Size, bool Snapped) LastCorrection { get; private set; }

    /// <summary>Onde desenhar: entre o tick anterior e o atual, mais o deslocamento de correção.</summary>
    public Vector2 RenderPosition => Vector2.Lerp(_previous, _position, (float)(_accumulator / World.TickSeconds)) + _offset;

    /// <summary>Começa (ou recomeça) na posição do servidor, parado.</summary>
    public void Reset(Vector2 position)
    {
        _position = _previous = position;
        _direction = Vector2.Zero;
        _offset = Vector2.Zero;
        _accumulator = 0;
        _history.Clear();
        _sent.Clear();
        _acks.Clear();
        Ready = true;
    }

    /// <summary>Intenção enviada com a sequência <paramref name="seq"/>; <paramref name="move"/> = nova direção (zero = parar), null = não é de movimento.</summary>
    public void Sent(uint seq, Vector2? move)
    {
        _sent.Add((seq, Tick + 1)); // vale a partir do próximo tick previsto
        if (move is { } dir)
        {
            var len = dir.Length();
            _direction = len > 1f ? dir / len : dir; // mesma normalização do World
        }
    }

    /// <summary><c>IntentAck</c>: a sequência <paramref name="seq"/> passou a valer no tick <paramref name="serverTick"/> do servidor.</summary>
    public void Acked(uint seq, long serverTick)
    {
        if (_acks.Count == 0 || seq > _acks[^1].Seq)
        {
            _acks.Add((seq, serverTick));
        }
    }

    /// <summary>Avança o relógio do cliente; roda os ticks vencidos e suaviza a correção.</summary>
    public void Advance(double deltaSeconds, PredictionParams p)
    {
        if (!Ready)
        {
            return;
        }

        _accumulator += Math.Max(0, deltaSeconds);
        var steps = 0;
        while (_accumulator >= World.TickSeconds && steps < 5)
        {
            _accumulator -= World.TickSeconds;
            steps++;
            Tick++;
            _previous = _position;
            _position = Step(_position, _direction, p);
            _history.Add((Tick, _direction));
        }

        if (_accumulator >= World.TickSeconds)
        {
            _accumulator %= World.TickSeconds;
        }

        while (_history.Count > HistoryTicks)
        {
            _history.RemoveAt(0);
        }

        _offset *= MathF.Exp(-SmoothingRate * (float)deltaSeconds);
        if (_offset.LengthSquared() < 1e-6f)
        {
            _offset = Vector2.Zero;
        }
    }

    /// <summary>Snapshot do servidor no tick <paramref name="serverTick"/> com a entidade em <paramref name="serverPosition"/>.</summary>
    public void Reconcile(long serverTick, Vector2 serverPosition, PredictionParams p)
    {
        if (!Ready)
        {
            Reset(serverPosition);
            return;
        }

        // Tick do cliente que corresponde ao tick do servidor.
        long clientTick;
        var ack = _acks.LastOrDefault(a => a.ServerTick <= serverTick);
        if (ack.Seq == 0)
        {
            // Nenhuma intenção nossa vale ainda no servidor: ele está onde começamos, sem andar.
            clientTick = _history.Count > 0 ? _history[0].Tick - 1 : Tick;
            if (_sent.Count == 0)
            {
                clientTick = Tick; // nunca mandamos nada: o servidor é a verdade
            }
        }
        else
        {
            var sent = _sent.FirstOrDefault(s => s.Seq == ack.Seq);
            if (sent.Seq == 0)
            {
                Snap(serverPosition); // confirmação de algo que já esquecemos
                return;
            }

            clientTick = sent.ClientTick + (serverTick - ack.ServerTick);
        }

        if (_history.Count > 0 && clientTick < _history[0].Tick - 1)
        {
            Snap(serverPosition); // longe demais no passado
            return;
        }

        var replayed = serverPosition;
        foreach (var (tick, dir) in _history)
        {
            if (tick > clientTick)
            {
                replayed = Step(replayed, dir, p);
            }
        }

        var error = replayed - _position;
        LastCorrection = (error.Length(), error.Length() > SnapDistance);
        if (error.Length() > SnapDistance)
        {
            Snap(replayed);
        }
        else
        {
            _offset -= error; // o desenho fica onde estava e escorrega para a posição certa
            _previous += error;
            _position = replayed;
        }

        // O que já foi confirmado antes deste ponto não é mais necessário.
        _history.RemoveAll(h => h.Tick <= clientTick - 1);
        if (ack.Seq != 0)
        {
            _sent.RemoveAll(s => s.Seq < ack.Seq);
            _acks.RemoveAll(a => a.Seq < ack.Seq);
        }
    }

    private void Snap(Vector2 position)
    {
        _position = _previous = position;
        _offset = Vector2.Zero;
    }

    private Vector2 Step(Vector2 from, Vector2 dir, PredictionParams p)
    {
        if (dir == Vector2.Zero || p.SpeedMultiplier <= 0)
        {
            return from;
        }

        var delta = dir * (p.Speed * p.SpeedMultiplier) * World.TickSeconds;
        return map is null ? from + delta : Collision.MoveCircle(map, from, p.Radius, delta);
    }
}
