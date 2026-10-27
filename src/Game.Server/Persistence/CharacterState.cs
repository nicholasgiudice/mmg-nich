using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Server.Persistence;

/// <summary>
/// O que se grava de um personagem (T-212): build (nível, XP, pontos, NH), equipamento e inventário. Imutável: é capturado na
/// thread do loop e gravado em outra. <see cref="Equipment"/> null = personagem novo, com o equipamento inicial da classe.
/// </summary>
public sealed record CharacterState(CharacterBuild Build, IReadOnlyDictionary<EquipSlot, ItemInstance>? Equipment, IReadOnlyList<ItemInstance?> Inventory)
{
    public static CharacterState New { get; } = new(new CharacterBuild(), null, []);

    /// <summary>Lê o estado de uma entidade criada por <see cref="Spawn"/> (null se não tem progressão).</summary>
    public static CharacterState? Capture(Entity e)
    {
        if (Leveling.BuildOf(e) is not { } build)
        {
            return null;
        }

        var equipment = e.TryGet<Equipment>()?.Items.ToDictionary(x => x.Slot, x => x.Item) ?? [];
        var inventory = e.TryGet<Inventory>()?.Slots.ToArray() ?? [];
        return new CharacterState(build, equipment, inventory);
    }

    /// <summary>
    /// Cria a entidade com o estado salvo: build validado (lança <see cref="ArgumentException"/> se não vale), equipamento e
    /// inventário. Itens que o catálogo não conhece mais, ou fora do espaço certo, são descartados e listados em
    /// <paramref name="dropped"/>. Vida e Mana começam cheias.
    /// </summary>
    public Entity Spawn(World world, ClassDefinition cls, Vector2 position, List<string>? dropped = null)
    {
        var e = Leveling.SpawnCharacter(world, cls, position, Build);
        if (Equipment is not null)
        {
            var equipment = new Equipment();
            foreach (var (slot, item) in Equipment)
            {
                if (world.Content.Items.TryGetValue(item.BaseId, out var def) && def.Slot == slot)
                {
                    equipment[slot] = item;
                }
                else
                {
                    dropped?.Add($"{slot}: {item.BaseId}");
                }
            }

            e.Add(equipment);
        }

        var inventory = e.Get<Inventory>();
        for (var i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i] is not { } item)
            {
                continue;
            }

            if (i < inventory.Capacity && world.Content.Items.ContainsKey(item.BaseId))
            {
                inventory[i] = item;
            }
            else
            {
                dropped?.Add($"inventário {i}: {item.BaseId}");
            }
        }

        ItemMath.Recompute(world.Content, cls, e.Get<Game.Core.Combat.Stats.CharacterStats>(), e.TryGet<CharacterProgress>(), e.Get<Equipment>());
        Leveling.RefreshDerived(world, e);
        var health = e.Get<Health>();
        health.Current = health.Max;
        if (e.TryGet<ResourcePool>() is { } pool && cls.Resource.Initial >= cls.Resource.Max)
        {
            pool.Current = pool.Max;
        }

        return e;
    }
}

/// <summary>
/// Formato do documento <c>characters.progression</c> (jsonb), versão <see cref="Version"/>. Nível e XP ficam nas colunas
/// <c>level</c> e <c>experience</c>. O documento vazio (<c>{}</c>, gravado pela API ao criar) é o personagem novo.
/// <code>
/// { "allocated": { "str": 0, "dex": 0, "agi": 0, "int": 0, "vit": 0 },
///   "skillRanks": { "warrior.heavy_strike": 1 },
///   "equipment": { "weapon": { "base": "weapon.sword", "level": 2, "rarity": "common", "affixes": [] } },
///   "inventory": [ { "slot": 3, "item": { ... } } ] }
/// </code>
/// </summary>
public static class CharacterDocument
{
    public const int Version = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public static string Write(CharacterState state)
    {
        var doc = new Doc
        {
            Allocated = state.Build.Allocated,
            SkillRanks = state.Build.SkillRanks.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(),
            Equipment = state.Equipment?.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => ItemDoc.Of(kv.Value)),
            Inventory = state.Inventory.Select((item, slot) => (item, slot)).Where(x => x.item is not null)
                .Select(x => new SlotDoc { Slot = x.slot, Item = ItemDoc.Of(x.item!) }).ToList(),
        };
        return JsonSerializer.Serialize(doc, Json);
    }

    /// <summary>Lê o documento; lança <see cref="JsonException"/> ou <see cref="InvalidDataException"/> se está corrompido.</summary>
    public static CharacterState Read(int version, string json, int level, long experience)
    {
        if (version != Version)
        {
            throw new InvalidDataException($"versão de progressão {version} desconhecida (esperada {Version})");
        }

        var doc = JsonSerializer.Deserialize<Doc>(json, Json) ?? throw new InvalidDataException("documento de progressão nulo");
        var build = new CharacterBuild
        {
            Level = level,
            Experience = checked((int)experience),
            Allocated = doc.Allocated ?? new AttributeSet(),
            SkillRanks = doc.SkillRanks ?? [],
        };
        if (doc.Equipment is null)
        {
            return CharacterState.New with { Build = build };
        }

        var inventory = new List<ItemInstance?>();
        foreach (var s in doc.Inventory ?? [])
        {
            if (s.Slot is < 0 or > 1000 || s.Item is null)
            {
                throw new InvalidDataException($"espaço de inventário inválido: {s.Slot}");
            }

            while (inventory.Count <= s.Slot)
            {
                inventory.Add(null);
            }

            inventory[s.Slot] = s.Item.ToItem();
        }

        return new CharacterState(build, doc.Equipment.ToDictionary(kv => kv.Key, kv => kv.Value.ToItem()), inventory);
    }

    private sealed class Doc
    {
        public AttributeSet? Allocated { get; set; }
        public Dictionary<string, int>? SkillRanks { get; set; }
        public Dictionary<EquipSlot, ItemDoc>? Equipment { get; set; }
        public List<SlotDoc>? Inventory { get; set; }
    }

    private sealed class SlotDoc
    {
        public int Slot { get; set; }
        public ItemDoc? Item { get; set; }
    }

    private sealed class ItemDoc
    {
        public string Base { get; set; } = "";
        public int Level { get; set; }
        public ItemRarity Rarity { get; set; }
        public List<string> Affixes { get; set; } = [];

        public static ItemDoc Of(ItemInstance i) => new() { Base = i.BaseId, Level = i.Level, Rarity = i.Rarity, Affixes = [.. i.Affixes] };

        public ItemInstance ToItem() => Base.Length > 0
            ? new ItemInstance(Base, Level, Rarity, Affixes)
            : throw new InvalidDataException("item sem base");
    }
}
