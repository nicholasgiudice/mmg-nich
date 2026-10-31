using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Simulation;
using Game.Data;
using MmoRpgClient.Match;

namespace Game.Client.Tests;

/// <summary>
/// T-221: o plano de NH da tela de habilidades (<see cref="SkillPlan.FromState"/>) só libera o botão + quando o Core
/// aceitaria a intenção, e dá o mesmo motivo de recusa que <see cref="Leveling.RaiseSkillRank"/>.
/// </summary>
public class SkillPlanTests
{
    private static readonly Lazy<ContentCatalog> Content = new(LoadContent);

    [Theory]
    [InlineData("class.warrior")]
    [InlineData("class.archer")]
    [InlineData("class.mage")]
    [InlineData("class.cleric")]
    public void RaiseBlockerMatchesCoreAtEachLevel(string classId)
    {
        var content = Content.Value;
        var cls = content.Classes[classId];
        foreach (var level in new[] { 1, 2, 3, 5, 9, 12, 15 })
        {
            var world = WorldBuilder.CreateDefault(1, null, content);
            var entity = Leveling.SpawnCharacter(world, cls, Vector2.Zero, new CharacterBuild { Level = level });

            // Sobe sempre a primeira habilidade que o plano libera, comparando cada habilidade com o Core antes.
            for (var step = 0; step < 40; step++)
            {
                var plan = SkillPlan.FromState(cls, content, entity.Get<AbilityState>(), level);
                Assert.True(plan.Enforce);
                Assert.Equal(content.Rules.Progression.SkillPointsAt(level), plan.PointBudget);
                Assert.Equal(entity.Get<CharacterProgress>().SkillPoints, plan.PointBudget - plan.PointsUsed);

                string? raised = null;
                foreach (var ability in plan.Abilities)
                {
                    var expected = plan.RaiseBlocker(ability.Id);
                    var copy = WorldBuilder.CreateDefault(1, null, content);
                    var probe = Leveling.SpawnCharacter(copy, cls, Vector2.Zero, Leveling.BuildOf(entity));
                    Assert.Equal(expected, Leveling.RaiseSkillRank(copy, probe, ability.Id));
                    if (expected is null && raised is null)
                    {
                        raised = ability.Id;
                    }
                }

                if (raised is null)
                {
                    break;
                }

                Assert.Null(Leveling.RaiseSkillRank(world, entity, raised));
            }
        }
    }

    [Fact]
    public void NewCharacterHasEverythingAtRankZero()
    {
        var content = Content.Value;
        var cls = content.Classes["class.warrior"];
        var world = WorldBuilder.CreateDefault(1, null, content);
        var entity = Leveling.SpawnCharacter(world, cls, Vector2.Zero);

        var plan = SkillPlan.FromState(cls, content, entity.Get<AbilityState>(), 1);

        Assert.All(plan.Abilities, a => Assert.Equal(0, plan.RankOf(a.Id)));
        Assert.Equal(0, plan.Floor(plan.Abilities[0]));
        Assert.Null(plan.RaiseBlocker("warrior.heavy_strike"));
        Assert.Equal(ProgressionRejectReason.LevelTooLow, plan.RaiseBlocker("warrior.living_wall"));
        Assert.Equal(ProgressionRejectReason.NotLearnable, plan.RaiseBlocker(cls.BasicAttack));
    }

    private static ContentCatalog LoadContent()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "classes.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        var dataDir = Path.Combine(dir ?? throw new DirectoryNotFoundException("data/ não encontrada"), "data");
        var sources = GameDataLoader.EnumerateContentFiles(dataDir)
            .Select(f => (f.Name, File.ReadAllText(f.Path)));
        var db = GameDataLoader.LoadSources(sources).GetDatabaseOrThrow();
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
        };
    }
}
