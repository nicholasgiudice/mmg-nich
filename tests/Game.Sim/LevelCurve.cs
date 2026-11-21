using System.Globalization;
using System.Numerics;
using System.Text;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Sim;

/// <summary>Valor (sem defesa nem crítico) de uma habilidade no NH do perfil: soma dos efeitos de dano, cura e escudo × acertos.</summary>
public sealed record AbilityValue(string Id, string Name, int Rank, double PerHit, int Hits)
{
    public double Total => PerHit * Hits;
}

/// <summary>Números de um perfil num nível (T-402): estatísticas, defesa contra um monstro do mesmo nível e habilidades.</summary>
public sealed record LevelStats
{
    public required string Class { get; init; }
    public required int Level { get; init; }
    public required int AttributePoints { get; init; }
    public required int UnspentSkillPoints { get; init; }
    public required CharacterStats Stats { get; init; }
    public required double Health { get; init; }
    public required double Mana { get; init; }
    public required double ManaRegen { get; init; }
    public required double Crit { get; init; }
    public required double AttackSpeedBonus { get; init; }

    /// <summary>Redução de dano físico e mágico contra um atacante do mesmo nível.</summary>
    public required double PhysicalReduction { get; init; }
    public required double MagicalReduction { get; init; }
    public required IReadOnlyList<AbilityValue> Abilities { get; init; }
}

/// <summary>Tempo para matar um monstro normal do próprio nível, sozinho e sem a assinatura (T-402).</summary>
public sealed record KillTime(string Class, int Level, string Monster, BatchSummary Batch, double MaxHealth, double XpPerKill)
{
    /// <summary>XP por minuto de luta (só o tempo de combate, sem descanso nem caminhada).</summary>
    public double XpPerMinute => Batch.Wins == 0 ? 0 : XpPerKill * 60 / Batch.TimeToWinMean;

    /// <summary>Dano recebido por luta em fração da vida máxima.</summary>
    public double DamageShare => MaxHealth <= 0 ? 0 : Batch.DamageTaken / MaxHealth;
}

/// <summary>
/// Curva do nível 1 ao máximo (T-402, <c>tasks/reports/balance-v0.18.md</c>): números do perfil esperado de cada classe,
/// quando ele bate nos tetos de AGI e de defesa e quanto tempo leva para matar um monstro normal do próprio nível.
/// </summary>
public static class LevelCurve
{
    public static readonly string[] Classes = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    /// <summary>Estratégia solo por classe, como nos testes de design (os de longe se afastam).</summary>
    public static readonly IReadOnlyDictionary<string, string> SoloStrategy = new Dictionary<string, string>
    {
        ["class.warrior"] = "rotation",
        ["class.archer"] = "kite",
        ["class.mage"] = "kite",
        ["class.cleric"] = "rotation",
    };

    /// <summary>Par focado do Clérigo solo (a cura do grupo não tem uso sozinho), como em <c>DesignHarness.SoloFocus</c>.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> SoloFocus = new Dictionary<string, string[]>
    {
        ["class.cleric"] = ["cleric.holy_chain", "cleric.sacred_shield"],
    };

    /// <summary>Monstros normais usados no tempo para matar: um médio dos Campos e um pesado das Ruínas.</summary>
    public static readonly string[] Monsters = ["monster.grey_wolf", "monster.skeleton_guard"];

    public static CharacterProfile Profile(int level) => CharacterProfile.Expected(level) with { Focus = SoloFocus };

    public static IEnumerable<int> Levels(int maxLevel) => new[] { 1 }.Concat(Enumerable.Range(1, maxLevel / 5).Select(i => i * 5)).Distinct();

    /// <summary>Monta o personagem do perfil esperado (com <see cref="SoloFocus"/>) e lê os números.</summary>
    public static LevelStats Measure(ContentCatalog content, string classId, int level, CharacterProfile? profile = null)
    {
        profile ??= Profile(level);
        var cls = content.Classes[classId];
        var world = WorldBuilder.CreateDefault(1, null, content);
        var e = CharacterProfiles.Spawn(world, cls, Vector2.Zero, profile);
        var attacker = EntityFactory.SpawnMonster(world, content.Monsters[Monsters[0]], new Vector2(1, 0), level);
        var s = e.Get<CharacterStats>();
        var r = content.Rules.Stats;
        var build = CharacterProfiles.BuildFor(content, cls, profile);
        var ranks = build.SkillRanks;
        var abilities = new[] { cls.BasicAttack }.Concat(cls.Abilities)
            .Select(id => Value(content, id, id == cls.BasicAttack ? 1 : ranks.GetValueOrDefault(id), s))
            .Where(a => a is not null && (a.Rank > 0 || a.Id == cls.BasicAttack))
            .Select(a => a!)
            .ToList();
        return new LevelStats
        {
            Class = classId,
            Level = s.Level,
            AttributePoints = content.Rules.Progression.AttributePointsAt(s.Level),
            UnspentSkillPoints = content.Rules.Progression.SkillPointsAt(s.Level) - ranks.Values.Sum(),
            Stats = (CharacterStats)s.Clone(),
            Health = e.Get<Health>().Max,
            Mana = e.TryGet<ResourcePool>()?.Max ?? 0,
            ManaRegen = cls.Resource.RegenPerSecond + StatMath.ManaRegenBonus(s, r),
            Crit = StatMath.CritChance(s, r),
            AttackSpeedBonus = StatMath.AttackSpeedFactor(s, r) - 1,
            PhysicalReduction = StatMath.DefenseReduction(world, e, attacker, DamageSchool.Physical),
            MagicalReduction = StatMath.DefenseReduction(world, e, attacker, DamageSchool.Fire),
            Abilities = abilities,
        };
    }

    /// <summary>Valor da habilidade no NH (0 = não aprendida); null se ela não causa dano, cura nem escudo.</summary>
    public static AbilityValue? Value(ContentCatalog content, string id, int rank, CharacterStats stats)
    {
        var def = content.AbilityAt(id, Math.Max(1, rank)) ?? content.Abilities[id];
        var perHit = 0.0;
        foreach (var effect in def.Effects)
        {
            perHit += effect switch
            {
                DamageEffect d => Scaled(d.Base, d.Scaling, stats),
                HealEffect h => Scaled(h.Base, h.Scaling, stats),
                ShieldEffect sh => Scaled(sh.Amount, sh.Scaling, stats),
                _ => 0,
            };
        }

        return perHit <= 0 ? null : new AbilityValue(id, content.Abilities[id].Name, rank, perHit, def.HitCount);
    }

    private static double Scaled(double baseValue, IReadOnlyDictionary<string, double> scaling, CharacterStats s) =>
        baseValue + scaling.Sum(kv => kv.Value * StatMath.Stat(s, kv.Key));

    /// <summary>Primeiro nível (1..máximo) em que a condição vale no perfil esperado da classe; null se nunca.</summary>
    public static int? FirstLevel(ContentCatalog content, string classId, Func<LevelStats, bool> condition)
    {
        for (var level = 1; level <= content.Rules.Progression.MaxLevel; level++)
        {
            if (condition(Measure(content, classId, level)))
            {
                return level;
            }
        }

        return null;
    }

    /// <summary>Encontro derivado: a classe sozinha contra um monstro no nível pedido, na arena dos Três Lobos.</summary>
    public static ContentCatalog WithDuel(ContentCatalog c, string classId, string monster, out string encounterId)
    {
        var id = $"encounter.t402_duel.{classId}.{monster}";
        encounterId = id;
        if (c.Encounters.ContainsKey(id))
        {
            return c;
        }

        var basis = c.Encounters["encounter.three_wolves"];
        var duel = basis with
        {
            Id = id,
            Name = "Duelo (T-402)",
            Players = [basis.Players[0] with { Class = classId }],
            Enemies = [new EnemySpawnDefinition { Monster = monster, Position = new PositionDefinition(17.5, 10.5) }],
            Defeat = basis.Defeat with { TimeLimitTicks = 180 * World.TickRate },
        };
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(duel), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }

    public static KillTime Kill(ContentCatalog content, string classId, int level, string monster, int runs, int parallelism = 0)
    {
        var duel = WithDuel(content, classId, monster, out var encounter);
        var options = new Dictionary<string, string> { ["class.mage:explosion"] = "mage.elemental_blast" };
        if (content.Classes[classId].Abilities.FirstOrDefault(id => content.Abilities[id].Signature) is { } signature)
        {
            // Ritmo de quem caça um monstro atrás do outro: a assinatura (recarga de 60–120 s) fica de fora.
            options[$"{classId}:skip"] = signature;
        }

        var batch = Batch.Run(duel, new RunSpec
        {
            EncounterId = encounter,
            Strategy = SoloStrategy[classId],
            Options = new BotOptions(options),
            Runs = runs,
            HazardSourceFactory = () => new TelegraphHazardSource(),
            Profile = Profile(level),
            MonsterLevel = level,
        }, parallelism);
        var rules = content.Rules.Progression;
        var def = content.Monsters[monster];
        return new KillTime(classId, level, monster, batch, Measure(content, classId, level).Health, rules.MonsterXp(level, def.Rank, level));
    }

    /// <summary>Relatório em Markdown (tabelas do <c>balance-v0.18.md</c>).</summary>
    public static string ToMarkdown(ContentCatalog content, int runs, int parallelism = 0)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        var levels = Levels(content.Rules.Progression.MaxLevel).ToList();
        var r = content.Rules.Stats;

        foreach (var cls in Classes)
        {
            var rows = levels.Select(l => Measure(content, cls, l)).ToList();
            var abilityIds = rows.SelectMany(x => x.Abilities.Select(a => a.Id)).Distinct().ToList();
            var names = abilityIds.Select(id => content.Abilities[id].Name).ToList();
            sb.AppendLine(inv, $"### {content.Classes[cls].Name}");
            sb.AppendLine();
            sb.AppendLine("| Nível | FOR/DES/AGI/INT/VIT | ARMA | Vida | Mana (regen/s) | Crítico | Vel. ataque | Red. física | Red. mágica | " + string.Join(" | ", names) + " |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|" + string.Concat(names.Select(_ => "---|")));
            foreach (var x in rows)
            {
                var s = x.Stats;
                var cells = abilityIds.Select(id => x.Abilities.FirstOrDefault(a => a.Id == id) is { } a
                    ? (a.Id == content.Classes[cls].BasicAttack ? "" : $"NH{a.Rank}: ") + (a.Hits > 1 ? $"{a.Hits} × {a.PerHit:0} = {a.Total:0}" : $"{a.Total:0}")
                    : "—");
                sb.AppendLine(inv, $"| {x.Level} | {s.Str}/{s.Dex}/{s.Agi}/{s.Int}/{s.Vit} | {s.WeaponDamage:0} | {x.Health:0} | {x.Mana:0} ({x.ManaRegen:0.0}) | {x.Crit:P1} | +{x.AttackSpeedBonus:P0} | {x.PhysicalReduction:P0} | {x.MagicalReduction:P0} | {string.Join(" | ", cells)} |");
            }

            sb.AppendLine();
        }

        sb.AppendLine("### Tetos");
        sb.AppendLine();
        sb.AppendLine(inv, $"Velocidade de ataque no máximo com AGI {r.AttackSpeedMax / r.AttackSpeedPerAgi:0}; crítico no máximo com AGI {(r.CritMax - r.CritBase) / r.CritPerAgi:0} (sem bônus de itens); defesa no máximo em {r.DefenseMax:P0}.");
        sb.AppendLine();
        sb.AppendLine("| Classe | AGI no nível máximo | Teto da vel. de ataque | Teto do crítico | Teto da defesa física | Teto da defesa mágica |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var cls in Classes)
        {
            var top = Measure(content, cls, content.Rules.Progression.MaxLevel);
            string At(Func<LevelStats, bool> f) => FirstLevel(content, cls, f) is { } l ? $"nível {l}" : "não bate";
            sb.AppendLine(inv, $"| {content.Classes[cls].Name} | {top.Stats.Agi} | {At(x => x.AttackSpeedBonus >= r.AttackSpeedMax - 1e-9)} | {At(x => x.Crit >= r.CritMax - 1e-9)} | {At(x => x.PhysicalReduction >= r.DefenseMax - 1e-9)} | {At(x => x.MagicalReduction >= r.DefenseMax - 1e-9)} |");
        }

        sb.AppendLine();
        foreach (var monster in Monsters)
        {
            var def = content.Monsters[monster];
            sb.AppendLine(inv, $"### Tempo para matar: {def.Name} do próprio nível ({runs} execuções)");
            sb.AppendLine();
            sb.AppendLine("Vida do monstro × fator de nível; sem a assinatura (ritmo de caça); XP por minuto só com o tempo de luta. Cada célula: vitória, tempo médio, dano recebido em % da vida máxima, XP/min.");
            sb.AppendLine();
            sb.AppendLine("| Nível | Vida do monstro | Potência | XP | " + string.Join(" | ", Classes.Select(c => content.Classes[c].Name)) + " |");
            sb.AppendLine("|---|---|---|---|" + string.Concat(Classes.Select(_ => "---|")));
            foreach (var level in levels)
            {
                var kills = Classes.Select(c => Kill(content, c, level, monster, runs, parallelism)).ToList();
                var hp = def.Health * content.Rules.Monsters.HealthFactor(level, def.Rank);
                var power = content.Rules.Monsters.PowerFactor(level, def.Rank);
                sb.Append(inv, $"| {level} | {hp:0} | ×{power:0.00} | {kills[0].XpPerKill:0} |");
                foreach (var k in kills)
                {
                    sb.Append(inv, $" {k.Batch.WinRate:P0}, {k.Batch.TimeToWinMean:0.0} s, {k.DamageShare:P0}, {k.XpPerMinute:0} |");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
