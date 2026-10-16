using System.Text.RegularExpressions;
using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Validação do conjunto de dados. Acumula todos os erros (não para no primeiro).</summary>
internal static partial class GameDataValidator
{
    [GeneratedRegex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)*$")]
    private static partial Regex IdPattern();

    public static void Validate(RawContent c, List<LoadError> errors)
    {
        var classes = UniqueIds(c.Classes, x => x.Id, "classe", errors);
        var abilities = UniqueIds(c.Abilities, x => x.Id, "habilidade", errors);
        var statuses = UniqueIds(c.Statuses, x => x.Id, "status", errors);
        var areas = UniqueIds(c.Areas, x => x.Id, "área", errors);
        var monsters = UniqueIds(c.Monsters, x => x.Id, "monstro", errors);
        UniqueIds(c.Arenas, x => x.Id, "arena", errors);
        UniqueIds(c.Encounters, x => x.Id, "encontro", errors);

        var refs = new Refs(abilities, statuses, areas, monsters);

        foreach (var e in c.Classes) { ValidateClass(e, refs, errors); }
        foreach (var e in c.Abilities) { ValidateAbility(e, refs, errors); }
        foreach (var e in c.Statuses) { ValidateStatus(e, refs, errors); }
        foreach (var e in c.Areas) { ValidateArea(e, refs, errors); }
        foreach (var e in c.Monsters) { ValidateMonster(e, refs, errors); }
        ValidateRules(c.Rules, errors);
        foreach (var e in c.Arenas) { ValidateArena(e, errors); }
        foreach (var e in c.Encounters)
        {
            ValidateEncounter(e, classes, monsters, c.Arenas.GroupBy(a => a.Value.Id).ToDictionary(g => g.Key, g => g.First().Value), errors);
        }
    }

    private sealed record Refs(HashSet<string> Abilities, HashSet<string> Statuses, HashSet<string> Areas, HashSet<string> Monsters);

    private static HashSet<string> UniqueIds<T>(List<Entry<T>> entries, Func<T, string> id, string kind, List<LoadError> errors)
    {
        var seen = new Dictionary<string, Entry<T>>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var value = id(entry.Value);
            if (!IdPattern().IsMatch(value))
            {
                errors.Add(new LoadError(entry.File, entry.Path + ".id",
                    $"Id '{value}' inválido: use snake_case com namespace por ponto (ex.: monster.grey_wolf)."));
            }

            if (seen.TryGetValue(value, out var first))
            {
                errors.Add(new LoadError(entry.File, entry.Path + ".id",
                    $"Id de {kind} duplicado '{value}' (primeira definição em {first.File}: {first.Path})."));
            }
            else
            {
                seen[value] = entry;
            }
        }

        return [.. seen.Keys];
    }

    private sealed class Ctx(string file, string path, List<LoadError> errors)
    {
        public string File { get; } = file;
        public string Path { get; } = path;

        public Ctx At(string sub) => new(File, Path + sub, errors);

        public void Error(string field, string message) =>
            errors.Add(new LoadError(File, field.Length == 0 ? Path : Path + (field[0] == '[' ? "" : ".") + field, message));

        public void Positive(string field, double value)
        {
            if (!(value > 0) || double.IsInfinity(value)) { Error(field, $"Deve ser maior que 0 (valor: {value})."); }
        }

        public void NonNegative(string field, double value)
        {
            if (!(value >= 0) || double.IsInfinity(value)) { Error(field, $"Deve ser maior ou igual a 0 (valor: {value})."); }
        }

        public void PositiveTicks(string field, int ticks)
        {
            if (ticks <= 0) { Error(field, $"Deve resultar em pelo menos 1 tick ({TickConversion.TicksPerSecond}/s)."); }
        }

        public void Exists(string field, string id, HashSet<string> set, string kind)
        {
            if (!set.Contains(id)) { Error(field, $"Referência inexistente: {kind} '{id}'."); }
        }
    }

    private static void ValidateClass(Entry<ClassDefinition> e, Refs refs, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.Positive("health", x.Health);
        ctx.NonNegative("speed", x.Speed);
        ctx.Positive("radius", x.Radius);
        ctx.Positive("resource.max", x.Resource.Max);
        if (!(x.Resource.Initial >= 0 && x.Resource.Initial <= x.Resource.Max))
        {
            ctx.Error("resource.initial", "Deve estar entre 0 e resource.max.");
        }

        ctx.NonNegative("resource.combatTimeout", x.Resource.CombatTimeoutTicks);
        ctx.Exists("basicAttack", x.BasicAttack, refs.Abilities, "habilidade");
        for (var i = 0; i < x.Abilities.Count; i++)
        {
            ctx.Exists($"abilities[{i}]", x.Abilities[i], refs.Abilities, "habilidade");
        }

        for (var i = 0; i < x.Consumables.Count; i++)
        {
            ctx.Exists($"consumables[{i}]", x.Consumables[i], refs.Abilities, "habilidade");
        }

        ValidateResourceRules(x, ctx);
    }

    private static void ValidateResourceRules(ClassDefinition x, Ctx ctx)
    {
        var ids = new HashSet<string>();
        for (var i = 0; i < x.ResourceRules.Count; i++)
        {
            var r = x.ResourceRules[i];
            var rc = ctx.At($".resourceRules[{i}]");
            if (!ids.Add(r.Id)) { rc.Error("id", $"Id de regra duplicado na classe: '{r.Id}'."); }
            if (!IdPattern().IsMatch(r.Id)) { rc.Error("id", $"Id inválido: '{r.Id}'."); }
            if (!Enum.IsDefined(r.Trigger)) { rc.Error("trigger", "Gatilho inválido."); }
            if (r.Amount == 0 && r.Ratio == 0) { rc.Error("amount", "Defina amount ou ratio diferente de 0."); }
            if (r.Ratio != 0 && r.Trigger is ResourceTrigger.StatusRemoved or ResourceTrigger.InterruptDone or ResourceTrigger.AbilityExecuted)
            {
                rc.Error("ratio", "Este gatilho não tem valor para proporção; use amount.");
            }

            if (r.MinValue is { } mv) { rc.NonNegative("minValue", mv); }
            if (r.MinDistance is { } mind) { rc.NonNegative("minDistance", mind); }
            if (r.MaxDistance is { } maxd) { rc.NonNegative("maxDistance", maxd); }
            if (r.MinDistance is { } a && r.MaxDistance is { } b && a > b) { rc.Error("minDistance", "Deve ser menor ou igual a maxDistance."); }
            if (r.MinEffectiveHealPercent is { } p)
            {
                if (r.Trigger != ResourceTrigger.HealDone) { rc.Error("minEffectiveHealPercent", "Só vale para o gatilho heal_done."); }
                if (!(p >= 0 && p <= 100)) { rc.Error("minEffectiveHealPercent", "Deve estar entre 0 e 100."); }
            }

            if (r.RemovedStatusTag is not null && r.Trigger != ResourceTrigger.StatusRemoved)
            {
                rc.Error("removedStatusTag", "Só vale para o gatilho status_removed.");
            }

            rc.NonNegative("internalCooldown", r.InternalCooldownTicks);
        }
    }

    private static void ValidateAbility(Entry<AbilityDefinition> e, Refs refs, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.NonNegative("cooldown", x.CooldownTicks);
        ctx.NonNegative("castTime", x.CastTimeTicks);
        if (x.Cost is { } cost)
        {
            ctx.NonNegative("cost.amount", cost.Amount);
        }

        if (x.Charges is { } charges && charges < 1) { ctx.Error("charges", "Deve ser pelo menos 1 (ausente = ilimitado)."); }
        ValidateTargeting(x.Targeting, ctx.At(".targeting"));
        if (x.Telegraph is { } tg) { ValidateTelegraph(tg, ctx.At(".telegraph")); }
        ValidateEffects(x.Effects, ctx, "effects", refs);
    }

    private static void ValidateTelegraph(TelegraphDefinition t, Ctx ctx)
    {
        ctx.PositiveTicks("delay", t.DelayTicks);
        switch (t.Shape)
        {
            case TelegraphShape.Circle:
                ctx.Positive("radius", t.Radius);
                break;
            case TelegraphShape.Cone:
                ctx.Positive("length", t.Length);
                if (!(t.Angle > 0 && t.Angle <= 360)) { ctx.Error("angle", "Deve estar em (0, 360]."); }
                break;
            case TelegraphShape.Line:
                ctx.Positive("length", t.Length);
                ctx.Positive("width", t.Width);
                break;
        }

        if (t.Charge && t.Shape != TelegraphShape.Line) { ctx.Error("charge", "Investida exige shape 'line'."); }
    }

    private static void ValidateTargeting(TargetingDefinition t, Ctx ctx)
    {
        switch (t)
        {
            case UnitTargeting u:
                ctx.Positive("range", u.Range);
                break;
            case MeleeArcTargeting m:
                ctx.Positive("range", m.Range);
                if (!(m.Angle > 0 && m.Angle <= 360)) { ctx.Error("angle", "Deve estar em (0, 360]."); }
                break;
            case LineTargeting l:
                ctx.Positive("length", l.Length);
                ctx.Positive("width", l.Width);
                break;
            case CircleAtPointTargeting c:
                ctx.Positive("range", c.Range);
                ctx.Positive("radius", c.Radius);
                break;
            case CircleSelfTargeting s:
                ctx.Positive("radius", s.Radius);
                break;
        }
    }

    private static void ValidateEffects(IReadOnlyList<EffectDefinition> effects, Ctx ctx, string field, Refs refs)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            var c = ctx.At($".{field}[{i}]");
            switch (effects[i])
            {
                case DamageEffect d:
                    c.NonNegative("base", d.Base);
                    foreach (var (tag, bonus) in d.BonusVsTags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) { c.Error("bonusVsTags", "Tag vazia."); }
                        if (!(bonus > -1)) { c.Error($"bonusVsTags.{tag}", "Deve ser maior que -1."); }
                    }

                    break;
                case HealEffect h:
                    c.NonNegative("base", h.Base);
                    break;
                case ShieldEffect s:
                    c.Positive("amount", s.Amount);
                    c.PositiveTicks("duration", s.DurationTicks);
                    break;
                case ApplyStatusEffect a:
                    c.Exists("status", a.Status, refs.Statuses, "status");
                    if (a.DurationTicks is { } d2) { c.PositiveTicks("duration", d2); }
                    break;
                case RemoveStatusEffect r:
                    if (string.IsNullOrWhiteSpace(r.Tag) && r.Tags.Count == 0) { c.Error("tag", "Informe 'tag' ou 'tags'."); }
                    if (r.Tags.Any(string.IsNullOrWhiteSpace)) { c.Error("tags", "Tag vazia."); }
                    if (r.Count < 1) { c.Error("count", "Deve ser pelo menos 1."); }
                    break;
                case DisplacementEffect m:
                    c.Positive("distance", m.Distance);
                    break;
                case ThreatEffect t:
                    c.NonNegative("amount", t.Amount);
                    if (t.TauntDurationTicks is { } taunt) { c.PositiveTicks("tauntDuration", taunt); }
                    break;
                case ResourceEffect:
                    break;
                case SpawnAreaEffect sa:
                    c.Exists("area", sa.Area, refs.Areas, "área");
                    break;
                case SummonEffect su:
                    c.Exists("monster", su.Monster, refs.Monsters, "monstro");
                    if (su.Count < 1) { c.Error("count", "Deve ser pelo menos 1."); }
                    c.NonNegative("radius", su.Radius);
                    if (su.MaxAlive < 0) { c.Error("maxAlive", "Deve ser maior ou igual a 0 (0 = sem limite)."); }
                    break;
            }
        }
    }

    private static void ValidateStatus(Entry<StatusDefinition> e, Refs refs, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.PositiveTicks("defaultDuration", x.DefaultDurationTicks);
        if (x.MaxStacks < 1) { ctx.Error("maxStacks", "Deve ser pelo menos 1."); }
        ctx.NonNegative("modifiers.speedMultiplier", x.Modifiers.SpeedMultiplier);
        ctx.NonNegative("modifiers.damageTakenMultiplier", x.Modifiers.DamageTakenMultiplier);
        ctx.NonNegative("modifiers.damageDealtMultiplier", x.Modifiers.DamageDealtMultiplier);
        ctx.NonNegative("modifiers.controlDurationMultiplier", x.Modifiers.ControlDurationMultiplier);
        if (x.Frontal is { } fr)
        {
            if (fr.ArcDegrees <= 0 || fr.ArcDegrees > 360) { ctx.Error("frontal.arcDegrees", "Deve estar entre 0 (exclusivo) e 360."); }
            ctx.NonNegative("frontal.damageTakenMultiplier", fr.DamageTakenMultiplier);
        }

        for (var i = 0; i < x.Periodic.Count; i++)
        {
            var c = ctx.At($".periodic[{i}]");
            c.PositiveTicks("interval", x.Periodic[i].IntervalTicks);
            ValidateEffects(x.Periodic[i].Effects, c, "effects", refs);
        }
    }

    private static void ValidateRules(RulesDefinition rules, List<LoadError> errors)
    {
        if (rules.Control is { } control)
        {
            var ctx = new Ctx("rules", "rules.control", errors);
            ctx.NonNegative("durationByRank.normal", control.DurationByRank.Normal);
            ctx.NonNegative("durationByRank.elite", control.DurationByRank.Elite);
            ctx.NonNegative("durationByRank.boss", control.DurationByRank.Boss);
            if (control.Diminishing.Steps.Count == 0) { ctx.Error("diminishing.steps", "Precisa de pelo menos um passo."); }
            foreach (var step in control.Diminishing.Steps)
            {
                if (step < 0 || step > 1) { ctx.Error("diminishing.steps", "Passos devem estar entre 0 e 1."); }
            }

            if (control.Diminishing.Steps.Count > 1) { ctx.PositiveTicks("diminishing.window", control.Diminishing.WindowTicks); }
        }
    }

    private static void ValidateArea(Entry<AreaDefinition> e, Refs refs, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.Positive("radius", x.Radius);
        ctx.PositiveTicks("duration", x.DurationTicks);
        if (x.Mode == AreaMode.Periodic) { ctx.PositiveTicks("interval", x.IntervalTicks); }
        else { ctx.NonNegative("interval", x.IntervalTicks); }
        ctx.NonNegative("armTime", x.ArmTimeTicks);
        if (x.ArmTimeTicks >= x.DurationTicks) { ctx.Error("armTime", "Deve ser menor que a duração da área."); }
        ValidateEffects(x.Effects, ctx, "effects", refs);
    }

    private static void ValidateMonster(Entry<MonsterDefinition> e, Refs refs, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.Positive("health", x.Health);
        ctx.NonNegative("speed", x.Speed);
        ctx.Positive("radius", x.Radius);
        foreach (var (school, _) in x.Resistances)
        {
            if (!Enum.GetNames<DamageSchool>().Any(n => string.Equals(n, school.Replace("_", ""), StringComparison.OrdinalIgnoreCase)))
            {
                ctx.Error($"resistances.{school}", $"Escola de dano desconhecida '{school}'.");
            }
        }

        for (var i = 0; i < x.Abilities.Count; i++)
        {
            ctx.Exists($"abilities[{i}]", x.Abilities[i], refs.Abilities, "habilidade");
        }

        if (string.IsNullOrWhiteSpace(x.Ai.Id)) { ctx.Error("ai.id", "Id do perfil de IA obrigatório."); }
        ValidatePhases(x, ctx, refs);
    }

    private static void ValidatePhases(MonsterDefinition x, Ctx ctx, Refs refs)
    {
        var ids = new HashSet<string>();
        var previous = double.PositiveInfinity;
        for (var i = 0; i < x.Phases.Count; i++)
        {
            var p = x.Phases[i];
            var pc = ctx.At($".phases[{i}]");
            if (!IdPattern().IsMatch(p.Id)) { pc.Error("id", $"Id de fase inválido: '{p.Id}'."); }
            if (!ids.Add(p.Id)) { pc.Error("id", $"Id de fase duplicado no monstro: '{p.Id}'."); }
            if (!(p.HealthPercent > 0 && p.HealthPercent <= 100)) { pc.Error("healthPercent", "Deve estar em (0, 100]."); }
            else if (i == 0 && p.HealthPercent != 100) { pc.Error("healthPercent", "A primeira fase deve ter healthPercent 100."); }
            else if (p.HealthPercent >= previous) { pc.Error("healthPercent", "Os limiares devem decrescer de uma fase para a seguinte."); }
            previous = p.HealthPercent;
            ValidateEffects(p.OnEnter, pc, "onEnter", refs);
            if (p.Abilities is { } list)
            {
                for (var j = 0; j < list.Count; j++)
                {
                    pc.Exists($"abilities[{j}]", list[j], refs.Abilities, "habilidade");
                }
            }
        }
    }

    private static void ValidateArena(Entry<ArenaDefinition> e, List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        ctx.Positive("scale", x.Scale);
        if (x.Lines.Count == 0)
        {
            ctx.Error("lines", "A arena precisa de pelo menos uma linha.");
            return;
        }

        var width = x.Lines[0].Length;
        if (width == 0) { ctx.Error("lines[0]", "Linha vazia."); }
        for (var i = 1; i < x.Lines.Count; i++)
        {
            if (x.Lines[i].Length != width)
            {
                ctx.Error($"lines[{i}]", $"Arena deve ser retangular: largura {x.Lines[i].Length}, esperado {width}.");
            }
        }

        if (!x.Lines.Any(l => l.Contains('.')))
        {
            ctx.Error("lines", "A arena precisa de pelo menos uma célula livre ('.').");
        }
    }

    private static void ValidateEncounter(
        Entry<EncounterDefinition> e,
        HashSet<string> classes,
        HashSet<string> monsters,
        Dictionary<string, ArenaDefinition> arenas,
        List<LoadError> errors)
    {
        var x = e.Value;
        var ctx = new Ctx(e.File, e.Path, errors);
        arenas.TryGetValue(x.Arena, out var arena);
        if (arena is null) { ctx.Error("arena", $"Referência inexistente: arena '{x.Arena}'."); }
        if (x.Players.Count == 0) { ctx.Error("players", "O encontro precisa de pelo menos um jogador."); }
        if (x.Enemies.Count == 0) { ctx.Error("enemies", "O encontro precisa de pelo menos um inimigo."); }
        if (x.Defeat.TimeLimitTicks is { } limit) { ctx.PositiveTicks("defeat.timeLimit", limit); }

        for (var i = 0; i < x.Players.Count; i++)
        {
            ctx.Exists($"players[{i}].class", x.Players[i].Class, classes, "classe");
            CheckSpawn(ctx, $"players[{i}].position", x.Players[i].Position, arena);
        }

        for (var i = 0; i < x.Enemies.Count; i++)
        {
            ctx.Exists($"enemies[{i}].monster", x.Enemies[i].Monster, monsters, "monstro");
            CheckSpawn(ctx, $"enemies[{i}].position", x.Enemies[i].Position, arena);
        }
    }

    private static void CheckSpawn(Ctx ctx, string field, PositionDefinition p, ArenaDefinition? arena)
    {
        if (arena is null || arena.Lines.Count == 0) { return; }
        var col = (int)Math.Floor(p.X);
        var row = (int)Math.Floor(p.Y);
        if (row < 0 || row >= arena.Lines.Count || col < 0 || col >= arena.Lines[row].Length)
        {
            ctx.Error(field, $"Posição ({p.X}, {p.Y}) fora da arena.");
        }
        else if (arena.Lines[row][col] == '#')
        {
            ctx.Error(field, $"Posição ({p.X}, {p.Y}) está dentro de uma parede.");
        }
    }
}
