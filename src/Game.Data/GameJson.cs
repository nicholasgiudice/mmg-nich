using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Game.Core.Definitions;

namespace Game.Data;

/// <summary>
/// Configuração do System.Text.Json para os dados de jogo. Mantém o Core livre de atributos JSON:
/// polimorfismo por "type" e conversão segundos -> ticks (propriedades "XxxTicks" lidas de "xxx") são
/// aplicados aqui por um modificador de metadados.
/// </summary>
public static class GameJson
{
    private const string TicksSuffix = "Ticks";

    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(Modify);

        return new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            AllowOutOfOrderMetadataProperties = true,
            RespectNullableAnnotations = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        };
    }

    private static void Modify(JsonTypeInfo info)
    {
        if (info.Type == typeof(TargetingDefinition))
        {
            info.PolymorphismOptions = Poly(
                (typeof(SelfTargeting), "self"),
                (typeof(UnitTargeting), "unit"),
                (typeof(MeleeArcTargeting), "melee_arc"),
                (typeof(LineTargeting), "line"),
                (typeof(CircleAtPointTargeting), "circle_at_point"),
                (typeof(CircleSelfTargeting), "circle_self"));
        }
        else if (info.Type == typeof(EffectDefinition))
        {
            info.PolymorphismOptions = Poly(
                (typeof(DamageEffect), "damage"),
                (typeof(HealEffect), "heal"),
                (typeof(ShieldEffect), "shield"),
                (typeof(ApplyStatusEffect), "apply_status"),
                (typeof(RemoveStatusEffect), "remove_status"),
                (typeof(DisplacementEffect), "displacement"),
                (typeof(InterruptEffect), "interrupt"),
                (typeof(ThreatEffect), "threat"),
                (typeof(ResourceEffect), "resource"),
                (typeof(SpawnAreaEffect), "spawn_area"),
                (typeof(SummonEffect), "summon"),
                (typeof(ChainEffect), "chain"),
                (typeof(ExtraTargetsEffect), "extra_targets"),
                (typeof(ReviveEffect), "revive"));
        }

        foreach (var property in info.Properties)
        {
            if (property.Name.EndsWith(TicksSuffix, StringComparison.Ordinal))
            {
                property.Name = property.Name[..^TicksSuffix.Length];
                if (property.PropertyType == typeof(int))
                {
                    property.CustomConverter = new SecondsToTicksConverter();
                }
                else if (property.PropertyType == typeof(int?))
                {
                    property.CustomConverter = new NullableSecondsToTicksConverter();
                }
            }
        }
    }

    private static JsonPolymorphismOptions Poly(params (Type Type, string Name)[] derived)
    {
        var options = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "type",
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };
        foreach (var (type, name) in derived)
        {
            options.DerivedTypes.Add(new JsonDerivedType(type, name));
        }

        return options;
    }

    private sealed class SecondsToTicksConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            TickConversion.SecondsToTicks(reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue((double)value / TickConversion.TicksPerSecond);
    }

    private sealed class NullableSecondsToTicksConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null : TickConversion.SecondsToTicks(reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue((double)value.Value / TickConversion.TicksPerSecond);
            }
        }
    }
}
