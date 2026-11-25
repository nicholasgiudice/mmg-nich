using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-503: ponto de surgimento com <c>announce</c> manda <see cref="FieldBossAnnounced"/> (nascimento e morte) a todos os
/// jogadores da zona, mesmo longe da área de interesse, e a ninguém de outra zona. O dado real ainda não tem chefe de
/// campo (vem na T-530): o teste marca o ponto do rato dos Campos.
/// </summary>
public class FieldBossAnnounceTests
{
    private const string Fields = "zone.vigil_fields";
    private const string BossMonster = "monster.field_rat";
    private const int Corpse = 20;
    private const int Respawn = 40;
    private const float Far = 45f;

    /// <summary>Conteúdo real com o ponto do rato dos Campos anunciado, de corpo e renascimento curtos.</summary>
    private static ContentCatalog Content()
    {
        var c = TestRig.RealContent.Value;
        var fields = c.Zones[Fields];
        var announced = false;
        fields = fields with
        {
            CorpseTicks = Corpse,
            MonsterSpawns = fields.MonsterSpawns
                .Select(s =>
                {
                    if (announced || s.Monster != BossMonster)
                    {
                        return s;
                    }

                    announced = true;
                    return s with { Announce = true, Count = 1, RespawnTicks = Respawn };
                })
                .ToList(),
        };
        var zones = c.Zones.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        zones[Fields] = fields;
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values, c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = zones,
            Shops = c.Shops,
        };
    }

    private static Vector2 GateCenter(OpenZone zone, string to)
    {
        var a = zone.Definition.Gates.First(g => g.To == to).Area;
        return new Vector2((float)(a.X + (a.Width / 2)), (float)(a.Y + (a.Height / 2)));
    }

    /// <summary>Tile livre (sem parede nem portão) a pelo menos <see cref="Far"/> tiles de todos os pontos em <paramref name="from"/>.</summary>
    private static Vector2 FarFreeTile(OpenZone zone, params Vector2[] from)
    {
        var map = zone.World.Map!;
        for (var y = map.Height - 2; y > 0; y--)
        {
            for (var x = map.Width - 2; x > 0; x--)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                if (from.All(f => Vector2.Distance(p, f) >= Far) && !map.IsBlocked(p) && zone.GateAt(p) is null)
                {
                    return p;
                }
            }
        }

        throw new InvalidOperationException($"sem tile livre a {Far} tiles em {zone.Id}");
    }

    /// <summary>Anúncios recebidos como (monstro, subárea, nasceu), sem o tick.</summary>
    private static List<(string Monster, string? Area, bool Spawned)> Announcements(TestClient c) =>
        c.All<EventsMessage>().SelectMany(m => m.Events).OfType<FieldBossAnnounced>().Select(e => (e.MonsterId, e.Area, e.Spawned)).ToList();

    private static void IntoFields(WorldRig w, TestClient c, params TestClient[] all)
    {
        w.Where(c).Entity.Get<Transform>().Position = GateCenter(w.City, Fields);
        w.Tick(1, all);
        Assert.Equal(Fields, w.Where(c).Zone.Id);
    }

    [Fact]
    public void Birth_and_death_reach_every_player_in_the_zone_but_nobody_in_another_zone()
    {
        using var w = new WorldRig(content: Content());
        var fields = w.Fields;
        var point = fields.Definition.MonsterSpawns.Single(s => s.Announce);
        var boss = new Vector2((float)point.Position.X, (float)point.Position.Y);

        var a = w.Join("aria");
        var b = w.Join("bruno");
        var outsider = w.Join("clara"); // fica na Cidade
        var all = new[] { a, b, outsider };
        IntoFields(w, a, all);
        IntoFields(w, b, all);

        // Pontos distantes entre si e do chefe: fora da área de interesse uns dos outros e do monstro.
        var posA = FarFreeTile(fields, boss);
        var posB = FarFreeTile(fields, boss, posA);
        w.Where(a).Entity.Get<Transform>().Position = posA;
        w.Where(b).Entity.Get<Transform>().Position = posB;
        w.Tick(3, all);

        var bossEntity = fields.World.Entities.First(e => e.TryGet<UnitInfo>()?.DefinitionId == BossMonster && Vector2.Distance(e.Get<Transform>().Position, boss) < 3);
        Assert.Null(a.Entity(bossEntity.Id)); // o chefe não está na área de interesse de ninguém
        Assert.Null(b.Entity(bossEntity.Id));
        Assert.Empty(Announcements(a));
        Assert.Empty(Announcements(b));

        // Morte.
        fields.World.Kill(bossEntity);
        w.Tick(3, all);
        var death = (BossMonster, point.Area, false);
        Assert.Equal(death, Assert.Single(Announcements(a)));
        Assert.Equal(death, Assert.Single(Announcements(b)));

        // Nascimento: depois do corpo e do prazo de renascimento.
        for (var i = 0; i < Corpse + Respawn + 10 && Announcements(a).Count < 2; i++)
        {
            w.Tick(1, all);
        }

        var birth = (BossMonster, point.Area, true);
        Assert.Equal([death, birth], Announcements(a));
        Assert.Equal([death, birth], Announcements(b));

        // Quem está em outra zona não recebe nada.
        Assert.Empty(Announcements(outsider));
    }

    [Fact]
    public void Points_without_announce_and_the_initial_spawn_say_nothing()
    {
        using var w = new WorldRig(); // dado real: nenhum ponto com announce
        var c = w.Join("aria");
        IntoFields(w, c, c);
        var rat = w.Fields.World.Entities.First(e => e.TryGet<UnitInfo>()?.DefinitionId == BossMonster);

        w.Fields.World.Kill(rat);
        w.Tick(Corpse + 1, c);

        Assert.Empty(Announcements(c));
    }
}
