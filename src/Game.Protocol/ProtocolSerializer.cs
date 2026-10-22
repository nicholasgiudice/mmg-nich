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
        MemoryPackFormatterProvider.Register(new ComponentFormatter());
        MemoryPackFormatterProvider.Register(new EntityDeltaFormatter());
    }

    /// <summary>
    /// Bytes de um componente replicável (tag + campos), para o servidor comparar estados (T-106): mesmo componente,
    /// mesmos bytes.
    /// </summary>
    public static byte[] SerializeComponent(IComponent component) => MemoryPackSerializer.Serialize(component);

    public static IComponent DeserializeComponent(ReadOnlySpan<byte> data) =>
        MemoryPackSerializer.Deserialize<IComponent>(data) ?? throw new MemoryPackSerializationException("Componente vazio.");

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

    private sealed class EntityDeltaFormatter : MemoryPackFormatter<EntityDelta>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref EntityDelta? value)
        {
            var v = value ?? throw new MemoryPackSerializationException("EntityDelta nulo.");
            writer.WriteVarInt(v.Id.Value);
            writer.WriteUnmanaged(v.Full);
            writer.WriteVarInt(v.Components.Length);
            foreach (var c in v.Components)
            {
                ComponentCodecs.Write(ref writer, c);
            }

            writer.WriteVarInt(v.RemovedComponents.Length);
            foreach (var t in v.RemovedComponents)
            {
                writer.WriteUnmanaged(t);
            }
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref EntityDelta? value)
        {
            var id = new EntityId(reader.ReadVarIntInt32());
            var full = reader.ReadUnmanaged<bool>();
            var components = new IComponent[Wire.Count(ref reader)];
            for (var i = 0; i < components.Length; i++)
            {
                components[i] = ComponentCodecs.Read(ref reader);
            }

            var removed = new byte[Wire.Count(ref reader)];
            for (var i = 0; i < removed.Length; i++)
            {
                removed[i] = reader.ReadUnmanaged<byte>();
            }

            value = new EntityDelta(id, full, components, removed);
        }
    }

    private sealed class ComponentFormatter : MemoryPackFormatter<IComponent>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IComponent? value) =>
            ComponentCodecs.Write(ref writer, value ?? throw new MemoryPackSerializationException("Componente nulo."));

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IComponent? value) =>
            value = ComponentCodecs.Read(ref reader);
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
