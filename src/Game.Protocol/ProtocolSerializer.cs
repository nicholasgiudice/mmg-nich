using System.Buffers;
using Game.Core.Events;
using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

/// <summary>
/// Ponto único de (des)serialização das mensagens (T-103). Registra no MemoryPack os formatadores dos tipos do Core
/// (<see cref="EntitySnapshot"/>, <see cref="GameEvent"/>), que não têm atributos do MemoryPack: o Core não conhece a rede.
/// </summary>
public static class ProtocolSerializer
{
    static ProtocolSerializer()
    {
        MemoryPackFormatterProvider.Register(new EntitySnapshotFormatter());
        MemoryPackFormatterProvider.Register(new GameEventFormatter());
    }

    public static byte[] Serialize(INetMessage message) => MemoryPackSerializer.Serialize(message);

    public static void Serialize(IBufferWriter<byte> output, INetMessage message) => MemoryPackSerializer.Serialize(output, message);

    /// <summary>Lê uma mensagem; lança <see cref="MemoryPackSerializationException"/> se os bytes são inválidos.</summary>
    public static INetMessage Deserialize(ReadOnlySpan<byte> data) =>
        MemoryPackSerializer.Deserialize<INetMessage>(data)
        ?? throw new MemoryPackSerializationException("Mensagem vazia.");

    /// <summary>Como <see cref="Deserialize"/>, mas devolve false em vez de lançar (dados vindos da rede).</summary>
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out INetMessage? message)
    {
        try
        {
            message = MemoryPackSerializer.Deserialize<INetMessage>(data);
            return message is not null;
        }
        catch (Exception ex) when (ex is MemoryPackSerializationException or ArgumentException or InvalidOperationException
            or IndexOutOfRangeException or OverflowException)
        {
            message = null;
            return false;
        }
    }

    private sealed class EntitySnapshotFormatter : MemoryPackFormatter<EntitySnapshot>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref EntitySnapshot? value)
        {
            if (value is null)
            {
                writer.WriteVarInt(-1);
                return;
            }

            writer.WriteVarInt(value.Id.Value);
            var replicated = value.Components.Where(c => c is IReplicated).ToList();
            writer.WriteVarInt(replicated.Count);
            foreach (var c in replicated)
            {
                ComponentCodecs.Write(ref writer, c);
            }
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref EntitySnapshot? value)
        {
            var id = reader.ReadVarIntInt32();
            if (id < 0)
            {
                value = null;
                return;
            }

            var n = Wire.Count(ref reader);
            var components = new IComponent[n];
            for (var i = 0; i < n; i++)
            {
                components[i] = ComponentCodecs.Read(ref reader);
            }

            value = EntitySnapshot.Create(new EntityId(id), components);
        }
    }

    private sealed class GameEventFormatter : MemoryPackFormatter<GameEvent>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref GameEvent? value)
        {
            if (value is null)
            {
                writer.WriteVarInt((ushort)0);
                return;
            }

            EventCodecs.Write(ref writer, value);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref GameEvent? value) =>
            value = EventCodecs.Read(ref reader);
    }
}
