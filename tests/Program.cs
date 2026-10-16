using System.Text.Json;
using System.Text.Json.Nodes;
using HotbarTools;
using Accord;
using WrathCombo.Engine;
using Newtonsoft.Json.Linq;
using WrathFork.Contract;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
var pack = new SetupPack();
Check(pack.Data.Jobs.Count == 32, "Expected 21 combat and 11 noncombat jobs.");
foreach (var job in pack.Data.Jobs)
{
    var original = JsonSerializer.Serialize(job);
    for (int mask = 0; mask < 16; mask++)
    {
        var settings = new AccordSettings();
        foreach (var variant in VariantDefinitions.All) settings.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
        var resolved = pack.Resolve(job, settings);
        var ids = resolved.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray();
        foreach (var id in SetupPack.ProtectionPresets(job.JobId))
            Check(ids.Contains(id) == settings.Enabled(VariantId.Protection), $"Protection leaked across variants: {job.Job}/{mask}/{id}");
        foreach (var id in SetupPack.BurstPresets(job.JobId))
            Check(ids.Contains(id) == settings.Enabled(VariantId.Burst), $"Burst has conflicting owners: {job.Job}/{mask}/{id}");
        if (job.JobId == 33)
            Check(ids.Contains(1011) == settings.Enabled(VariantId.ClassMechanics) && ids.Contains(1044) == settings.Enabled(VariantId.ClassMechanics), "AST draw configuration disagrees with runtime.");
        if (job.JobId == 27)
            Check(new[] { 17028, 17056, 17029, 17059 }.All(id => ids.Contains(id) == settings.Enabled(VariantId.Support)), "SMN support does not obey its sole owner.");
        Check(ids.Distinct().Count() == ids.Length, $"Duplicate enabled presets: {job.Job}/{mask}");
        Check(ids.All(job.OwnedPresetIds.Contains), $"Preset ownership violation: {job.Job}/{mask}");
        var current = job.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).Append(-123).ToHashSet();
        var uncontrolled = current.Except(pack.ControlledPresets(job)).ToHashSet();
        var reverse = new AccordSettings();
        foreach (var definition in VariantDefinitions.All) reverse.Variants[definition.Id] = !settings.Enabled(definition.Id);
        pack.ApplyMembership(job, reverse, current);
        pack.ApplyMembership(job, settings, current);
        Check(current.Except(pack.ControlledPresets(job)).ToHashSet().SetEquals(uncontrolled), "Feature-only switch changed unrelated presets.");
        Check(current.Intersect(pack.ControlledPresets(job)).ToHashSet().SetEquals(ids.Intersect(pack.ControlledPresets(job))), "Switch result depends on previous variants.");
        Check(resolved.Slots.Length <= 12, $"Layout overflow: {job.Job}/{mask}");
        Check(resolved.Slots.Select(s => (s.RegularBar, s.RegularSlot)).Distinct().Count() == resolved.Slots.Length, $"Layout overlap: {job.Job}/{mask}");
        Check(resolved.Slots.All(s => KeyboardLayout.Positions.Contains(new(s.RegularBar - 1, s.RegularSlot - 1))), $"Unknown layout position: {job.Job}/{mask}");
        if (job.JobId is >= 8 and <= 18) Check(resolved.Slots.Length == 12, "Noncombat page must remain complete.");
        if (job.Job == "DNC")
        {
            Check(ids.Contains(4018) && ids.Contains(4045), "DNC completion must survive manual variants.");
            Check(resolved.Slots.Any(s => s.Action == "Technical Step") && resolved.Slots.Any(s => s.Action == "Standard Step"), "Missing manual dance initiation.");
        }
        if (job.Job == "AST") Check(resolved.Slots.Any(s => s.Action == "Astral Draw") && resolved.Slots.Any(s => s.Action == "Divination"), "Missing AST initiators.");
        if (job.Job == "BRD")
        {
            Check(ids.Contains(3070) == settings.Enabled(VariantId.Support), "Minne must have one owner: support.");
            Check(ids.Contains(3069) == settings.Enabled(VariantId.Protection), "Troubadour belongs to protection.");
        }
    }
    Check(JsonSerializer.Serialize(job) == original, "Resolution mutated its baseline.");
    foreach (uint baseClass in job.BaseClasses) Check(pack.Find(baseClass) == job, "Base class lost setup inheritance.");
    if (job.JobId >= 19)
        Check(JobIntents.Jobs.TryGetValue(job.JobId, out var descriptions) && descriptions.Length == 4 && descriptions.All(s => !string.IsNullOrWhiteSpace(s)), $"Incomplete intent review: {job.Job}");
}

// Contract failures must leave the original snapshot untouched.
var snapshot = JObject.Parse("{ 'EnabledActionsV6':[1,99], 'CustomIntValuesV6':{'managed':20,'unrelated':71}, 'CustomBoolValuesV6':{'toggle':false}, 'CustomIntArrayValuesV6':{'priority':[1,2]}, 'ForkFeatures':{}, 'CustomHealStack':['Self'] }");
var savedSnapshot = snapshot.ToString();
JObject Patch(JObject changes) => new() { ["Settings"] = changes, ["Enable"] = new JArray(2), ["Disable"] = new JArray(1) };
var good = SettingsPatch.Apply(snapshot, Patch(JObject.Parse("{'CustomIntValuesV6':{'managed':25}, 'CustomHealStack':['FocusTarget','Self']}")), new HashSet<int> {1,2,99});
Check(good["CustomIntValuesV6"]!.Value<int>("unrelated") == 71, "Patch overwrote an unmanaged detail.");
Check(good["EnabledActionsV6"]!.Values<int>().ToHashSet().SetEquals(new[] {2,99}), "Patch lost an unrelated preset.");
Check(good["CustomHealStack"]!.Count() == 2, "Target priorities must allow a different length.");
foreach (var bad in new[] {
    "{'RotationConfig':{'Enabled':true}}", "{'CustomIntValuesV6':{'unknown':1}}",
    "{'CustomIntValuesV6':{'managed':'wrong'}}", "{'CustomIntValuesV6':{'managed':2147483648}}",
    "{'CustomBoolValuesV6':{'toggle':1}}", "{'CustomIntArrayValuesV6':{'priority':[true]}}",
    "{'ForkFeatures':{'38':{'InventedFeature':true}}}", "{'ForkFeatures':{'999':{'DncStandardInitiation':true}}}",
    "{'CustomHealStack':[null]}" })
{
    try { SettingsPatch.Apply(snapshot, Patch(JObject.Parse(bad)), new HashSet<int> {1,2,99}); Check(false, "Invalid API patch accepted: " + bad); }
    catch (ArgumentException) { Check(snapshot.ToString() == savedSnapshot, "Rejected patch mutated the snapshot."); }
}
Check(snapshot.ToString() == savedSnapshot, "Successful patch mutated its input.");
foreach (var job in pack.Data.Jobs)
    for (int mask = 0; mask < 16; mask++)
    {
        var settings = new AccordSettings();
        foreach (var variant in VariantDefinitions.All) settings.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
        var features = SetupPack.EngineFeatures(job.JobId, settings);
        if (job.JobId < 19) { Check(features.Count == 0, "Noncombat class gained rotation features."); continue; }
        Check(features[Feature.BurstContinuation], "Manual burst must retain confirmed continuation.");
        if (job.JobId == 38)
            Check(features[Feature.DncStandardInitiation] == settings.Enabled(VariantId.ClassMechanics) && features[Feature.DncTechnicalInitiation] == settings.Enabled(VariantId.ClassMechanics) && features[Feature.DncCuringWaltz] == settings.Enabled(VariantId.Support), "DNC granular feature mapping has conflicting ownership.");
        if (job.JobId == 33) Check(features[Feature.AstCardsOnStDamage] && features[Feature.AstCardsOnAoeDamage], "Accepted AST cards must remain usable from damage in manual mechanics.");
        if (job.JobId == 23) Check(features[Feature.BardSongInitiation] == settings.Enabled(VariantId.ClassMechanics), "Song initiation has wrong owner.");
    }

var state = new IntentHandoff();
Check(!state.Active(23, 1000), "Displaying/pressing an unavailable initiator cannot open a handoff.");
state.Accept(23, 1000);
Check(state.Active(23, 1001), "Confirmed initiation must open follow-through.");
Check(!state.Active(31, 1001), "Handoff leaked across jobs.");
state.Accept(23, 2000);
Check(!state.Active(23, 31000), "Follow-up actions prolonged the burst indefinitely.");
state.Accept(23, 40000);
state.Reset();
Check(!state.Active(23, 40001), "Reset must discard accepted intent.");
state.ObserveInput(31, 17209, 50000);
Check(!state.Active(31, 50001), "An input alone incorrectly activated burst.");
Check(!state.ConfirmInput(31, 7414, 50001), "An unrelated action confirmed manual intent.");
Check(!state.ConfirmInput(31, 17209, 52000), "An expired/failed input stayed queued.");
state.ObserveInput(31, 17209, 60000);
Check(state.ConfirmInput(31, 17209, 60100) && state.Active(31, 60101), "Matching confirmed manual input did not hand off.");
foreach (var job in pack.Data.Jobs)
{
    state.Reset();
    state.Accept(job.JobId, 1000);
    int duration = BurstPolicy.Duration(job.JobId);
    Check(state.Active(job.JobId, 1000) == (duration > 0), "A single-initiator job acquired an unnecessary timer.");
    if (duration == 0) continue;
    Check(state.Active(job.JobId, 1000 + duration - 1) && !state.Active(job.JobId, 1000 + duration), "Handoff boundary is incorrect.");
    state.Accept(job.JobId, 1000 + duration - 1);
    Check(!state.Active(job.JobId, 1000 + duration), "A late follow-up extended the commitment.");
    Check(!BurstPolicy.Allows(job.JobId, -1), "Handoff enabled an unrelated preset.");
    foreach (int id in job.OwnedPresetIds.Where(id => BurstPolicy.Allows(job.JobId, id)))
        Check(job.AutoBurst!.Enable.Contains(id), $"Handoff includes an action outside reviewed burst ownership: {job.Job}/{id}");
}
Check(!BurstPolicy.Allows(20, 9013) && !BurstPolicy.Allows(22, 6105) && !BurstPolicy.Allows(30, 10011), "Burst improperly initiated Perfect Balance, Life or TCJ.");
Check(!BurstPolicy.Allows(23, 3009) && !BurstPolicy.Allows(31, 8100), "Handoff changed a rotation parent.");
Check(BurstPolicy.Allows(23, 3017) && BurstPolicy.Allows(23, 3032) && BurstPolicy.Allows(37, 7204), "Missing ST/AoE buff continuation.");

var maps = Defaults.Maps();
maps.Add(new SlotMap { Id = "custom-shared", Profile = "All", RegularBar = 5, RegularSlot = 2, CrossSet = 5, CrossSlot = 7, SharedAcrossJobs = true, Enabled = false });
var kept = new Position(0, 0);
var cleanup = HotbarCleanup.Plan(19, [kept], maps, bar => bar == 6, _ => new SlotValue(1, 123));
Check(cleanup.All(e => e.Position != kept && e.Position.Bar != 6), "Cleanup removed layout/native shared controls.");
foreach (var shared in maps.Where(m => m.SharedAcrossJobs))
    Check(cleanup.All(e => e.Position != shared.Regular && e.Position != shared.Cross), "Cleanup removed Bridge shared controls, including disabled pairs.");
Check(cleanup.Any(e => e.Position == new Position(4, 0)), "Cleanup did not clear an undesigned filled slot.");
var edits = Enumerable.Range(0, 4).Select(i => new SlotEdit(19, new(0, i), new(1, (uint)i + 1), new(1, (uint)i + 10))).ToList();
for (int failAt = 0; failAt <= edits.Count; failAt++)
{
    var values = edits.ToDictionary(e => e.Position, e => e.Before);
    var rollback = new List<Position>();
    bool routesRestored = false;
    int written = 0;
    try
    {
        HotbarTransaction.Apply(edits, (edit, undo) =>
        {
            values[edit.Position] = undo ? edit.Before : edit.After;
            if (undo) rollback.Add(edit.Position);
            else if (written++ == failAt) throw new IOException("Injected write/read-back failure");
        }, () => throw new IOException("Injected Bridge rejection"), () => routesRestored = true);
        Check(false, "Transaction should have failed.");
    }
    catch (AggregateException)
    {
        Check(edits.All(e => values[e.Position] == e.Before), "Failure left partially applied hotbars.");
        Check(routesRestored, "Failure did not restore Bridge routes.");
        Check(rollback.SequenceEqual(edits.Take(Math.Min(failAt + 1, edits.Count)).Reverse().Select(e => e.Position)), "Rollback order is wrong.");
    }
}
int undoAttempts = 0;
bool routeUndoAttempted = false;
try
{
    HotbarTransaction.Apply(edits, (_, undo) => { if (undo) { undoAttempts++; throw new IOException("Undo failed"); } },
        () => throw new IOException("Route update failed"), () => { routeUndoAttempted = true; throw new IOException("Route undo failed"); });
}
catch (AggregateException ex)
{
    Check(undoAttempts == edits.Count && routeUndoAttempted, "A rollback failure skipped other recovery attempts.");
    Check(ex.InnerExceptions.Count == 6, "Recovery failures were hidden.");
}
checks += SeparationTests.Run();
if (args.Length > 0) checks += EngineSchema.Verify(args[0], pack);
Console.WriteLine($"Passed {checks} assertions across 512 job/variant combinations, burst handoff, shared-slot cleanup and transaction failure recovery.");
