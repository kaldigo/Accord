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
// Unconfigured fork options must preserve upstream initiation and add no new routing.
var upstreamEnabled = new HashSet<Feature> { Feature.DncStandardInitiation, Feature.DncTechnicalInitiation, Feature.BardSongInitiation, Feature.DncCuringWaltz };
upstreamEnabled.UnionWith(Enum.GetValues<Feature>().Where(f=>f.ToString().StartsWith("PvP") && f.ToString().EndsWith("Initiation")));
foreach (var feature in Enum.GetValues<Feature>())
    Check(FeatureDefaults.Enabled(feature) == upstreamEnabled.Contains(feature), "Unconfigured fork changed upstream behaviour: " + feature);
foreach(var sprint in new[]{new SlotValue(1,3),new SlotValue(1,29057),new SlotValue(10,4)})
{
 Check(ModeActions.HasSharedSprint(p=>p==new Position(0,11),p=>sprint),"Shared Sprint binding not recognized.");
 Check(ModeActions.HasSharedSprint(p=>p.Bar==9,p=>p.Slot==7?sprint:default),"Sprint on a shared whole bar not recognized.");
 Check(!ModeActions.HasSharedSprint(p=>p.Bar==9,p=>p.Bar==1?sprint:default),"Job fallback was mistaken for shared Sprint.");
}
Check(!ModeActions.IsSprint(new SlotValue(10,3)) && !ModeActions.IsSprint(new SlotValue(9,4)),"Other general actions or macros mistaken for Sprint.");
var pack = new SetupPack();
var sharedChoices=new AccordSettings {ActiveJobs=[19,38],LiveMode=true,ClearOtherActions=true};
sharedChoices.Variants[VariantId.Burst]=true;
sharedChoices.PvP.ActiveJobs=[31];sharedChoices.PvP.Variants[VariantId.Support]=true;
sharedChoices.PvP.LastHotbarBackup="native-pvp-backup";
var sharedPvp=sharedChoices.SharedPvP();
Check(sharedPvp.ActiveJobs.SetEquals([19u,38u]) && !sharedPvp.Enabled(VariantId.Support) && sharedPvp.Enabled(VariantId.Burst),"Shared settings did not use PvE choices exclusively.");
Check(sharedPvp.LiveMode && sharedPvp.ClearOtherActions && sharedPvp.LastHotbarBackup=="native-pvp-backup","Shared preferences lost native mode history.");
var requestState=new PvPRequestState();
requestState.Context(31,1,true);requestState.Observe(CombatRequest.Offense,41469,1000);
Check(!requestState.Active(CombatRequest.Offense,1001),"Press without confirmed execution started follow-through.");
requestState.Confirm(29409,1002);Check(!requestState.Active(CombatRequest.Offense,1003),"Unrelated execution accepted a request.");
requestState.Confirm(41469,1004);Check(requestState.Active(CombatRequest.Offense,1005),"Confirmed start lost its follow-through.");
requestState.Confirm(41469,19000);Check(!requestState.Active(CombatRequest.Offense,21005),"Follow-up extended the accepted window.");
requestState.Observe(CombatRequest.Mechanic,29414,22000);requestState.Confirm(29414,26000);
Check(!requestState.Active(CombatRequest.Mechanic,26001),"Expired input became an accepted request.");
requestState.Observe(CombatRequest.Defense,29507,27000);requestState.Confirm(29507,27001);requestState.Complete(CombatRequest.Defense);
Check(!requestState.Active(CombatRequest.Defense,27002),"Completed sequence started again.");
requestState.Observe(CombatRequest.Offense,41469,28000);requestState.Confirm(41469,28001);requestState.Context(31,2,true);
Check(!requestState.Active(CombatRequest.Offense,28002),"Request leaked across characters.");
// Every variant compacts keyboard functions without changing controller ownership.
foreach (var preset in pack.Data.Jobs)
for (int mask = 0; mask < 16; mask++)
{
    var choices = new AccordSettings();
    foreach (var variant in VariantDefinitions.All) choices.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
    var resolved = pack.Resolve(preset, choices);
    foreach(var targetJob in new[]{preset.JobId}.Concat(preset.BaseClasses))
    foreach(bool existingExtra in new[]{false,true})
    foreach(bool sharedControllers in new[]{false,true})
    foreach(bool sprintFallback in new[]{false,true})
    {
        var planningMaps=Defaults.Maps();
        if(existingExtra && targetJob is 1 or 19)planningMaps.Add(ExtraJobMappings.For(targetJob));
        bool Protected(Position p)=> (sharedControllers && p.Bar>=10) || p.Bar is >=2 and <10 ||
            planningMaps.Any(m=>m.SharedAcrossJobs && (m.Regular==p || m.Cross==p));
        var plan=PveLayoutPlan.Create(targetJob,resolved,planningMaps,Protected,sprintFallback);
        if(Defaults.IsCombat(targetJob))Check(plan.Count(e=>e.Slot?.Action=="Sprint")== (sprintFallback?1:0),"Sprint fallback missing or duplicated.");
        var required=resolved.Slots.Where(s=>s.Action!="Empty").ToArray();
        foreach(var slot in required)
            Check(plan.Count(e=>ReferenceEquals(e.Slot,slot))==1,$"{preset.Job}/{mask}: required action missing or duplicated: {slot.Action}");
        Check(plan.All(e=>!Protected(e.Regular) && (e.Cross is null || !Protected(e.Cross.Value))),"Setup writes a protected destination.");
        Check(!sharedControllers || plan.All(e=>e.Cross is null),"Shared controller bars still required by keyboard setup.");
        var plannedRoutes=plan.Where(e=>e.Registered).ToDictionary(e=>e.Map.Id,e=>e.Regular);
        LayoutRoutes.Effective(planningMaps.Where(m=>m.Applies(targetJob)),plannedRoutes);
        Check(plan.Select(e=>e.Regular).Distinct().Count()==plan.Length,"Keyboard-only plan overlaps actions.");
        var anchorSlots=resolved.Slots.Where(s=>s.LogicalSlot is 1 or 2 ||
            ((Defaults.IsHealer(preset.JobId) || preset.JobId is 19 or 21 or 32 or 37) && s.LogicalSlot is 9 or 10) ||
            (Defaults.IsCombat(preset.JobId) && !Defaults.IsHealer(preset.JobId) && s.LogicalSlot==4 && s.Action!="Empty")).ToArray();
        var anchors=anchorSlots.Select(s=>new Position(s.RegularBar-1,s.RegularSlot-1)).ToHashSet();
        foreach(var anchor in anchorSlots)Check(plan.Single(e=>ReferenceEquals(e.Slot,anchor)).Regular==new Position(anchor.RegularBar-1,anchor.RegularSlot-1),"A fixed intent moved on a job/base class.");
        var occupiedPlan=plan.Where(e=>e.Slot is not null && e.Slot.Action!="Empty").Select(e=>e.Regular).ToHashSet();
        foreach(var bar in occupiedPlan.Where(p=>!anchors.Contains(p)).GroupBy(p=>p.Bar))
            foreach(var slot in Enumerable.Range(0,bar.Max(p=>p.Slot)+1).Select(s=>new Position(bar.Key,s)))
                Check(Protected(slot) || anchors.Contains(slot) || occupiedPlan.Contains(slot),"Physical keyboard gap before a movable action.");
        if(required.Any(s=>s.LogicalSlot==13))
            Check(plan.Single(e=>e.Slot?.LogicalSlot==13).Regular.Bar<2,"Paladin knockback action still uses a linked utility bar.");
        foreach(bool cleanupEnabled in new[]{false,true})
        {
            var keep=plan.SelectMany(e=>e.Cross is {} cross ? new[]{e.Regular,cross} : new[]{e.Regular}).ToHashSet();
            var cleared=cleanupEnabled ? HotbarCleanup.Plan(targetJob,keep,planningMaps,
                bar=>sharedControllers && bar>=10,_=>new SlotValue(1,123)).Where(e=>!Protected(e.Position)).ToArray() : [];
            Check(cleared.All(e=>!keep.Contains(e.Position)),"Cleanup removes a planned keyboard action.");
        }
    }
    var layout = KeyboardLayout.Compile(resolved, true);
    Check(layout.Values.Distinct().Count() == KeyboardLayout.Positions.Length, "Compaction overlaps keyboard controls.");
    Check(layout.All(pair => pair.Key.Bar == pair.Value.Bar), "Compaction moved a function to another keyboard bar.");
    var mapsForJob = ExtraJobMappings.Merge(Defaults.Maps(), resolved.Slots.Where(s=>s.LogicalSlot>12).Select(s=>ExtraJobMappings.For(preset.JobId,s.LogicalSlot)), new())
        .Where(m => m.Applies(preset.JobId)).ToList();
    var routes = mapsForJob.Where(m => !m.SharedAcrossJobs).ToDictionary(m => m.Id, m => layout.GetValueOrDefault(m.Regular, m.Regular));
    var routed = LayoutRoutes.Effective(mapsForJob, routes);
    foreach (var map in mapsForJob)
    {
        var after = routed.Single(m => m.Id == map.Id);
        Check(after.Cross == map.Cross, "Compaction moved a controller function.");
        Check(!map.SharedAcrossJobs || after.Regular == map.Regular, "Compaction moved a shared control.");
    }
    foreach (var slot in resolved.Slots.Where(s => s.Action != "Empty"))
    {
        var source = new Position(slot.RegularBar - 1, slot.RegularSlot - 1);
        var map = mapsForJob.Single(m => m.Regular == source);
        Check(routed.Single(m => m.Id == map.Id).Regular == layout.GetValueOrDefault(source, source), "Bridge lost a moved action.");
    }
    Check(layout[new(0, 0)] == new Position(0, 0) && layout[new(0, 1)] == new Position(0, 1), "Compaction moved ST/AoE.");
    var locked = resolved.Slots.Where(s => s.LogicalSlot is 1 or 2 ||
        ((Defaults.IsHealer(preset.JobId) || preset.JobId is 19 or 21 or 32 or 37) && s.LogicalSlot is 9 or 10) ||
        (Defaults.IsCombat(preset.JobId) && !Defaults.IsHealer(preset.JobId) && s.LogicalSlot == 4 && s.Action != "Empty"))
        .Select(s => new Position(s.RegularBar - 1, s.RegularSlot - 1)).ToHashSet();
    var occupied = resolved.Slots.Where(s => s.Action != "Empty").Select(s => layout.GetValueOrDefault(new(s.RegularBar - 1, s.RegularSlot - 1), new(s.RegularBar - 1, s.RegularSlot - 1))).ToHashSet();
    foreach (var bar in KeyboardLayout.Positions.GroupBy(p => p.Bar))
    {
        bool emptySeen = false;
        foreach (var position in bar.Where(p => !locked.Contains(p)))
        {
            if (!occupied.Contains(position)) emptySeen = true;
            else Check(!emptySeen, "Compaction left a movable gap within a keyboard bar.");
        }
    }
}
var scope = new AccordSettings();
Check(pack.SelectedJobs(scope).Length == 0, "New installations must wait for a class selection.");
scope.PreparedJobs[19] = 1;
Check(!pack.IsActive(1, scope) && !pack.IsActive(19, scope), "Prepared history must not opt classes in.");
scope.ActiveJobs = null;
Check(pack.SelectedJobs(scope).Length == 0, "Legacy null selection must default to none.");
scope.ActiveJobs = new HashSet<uint>();
Check(!pack.IsActive(19, scope), "Explicitly selecting none must not reactivate prepared classes.");
scope.ActiveJobs = new HashSet<uint> { 1, 19, 38, 999 };
Check(pack.SelectedJobs(scope).Select(j => j.JobId).ToHashSet().SetEquals(new uint[] {19, 38}), "Selection must deduplicate base classes and ignore unsupported IDs.");
Check(!pack.IsActive(33, scope), "Unselected AST must remain unmanaged.");
var savedScope = Newtonsoft.Json.JsonConvert.DeserializeObject<AccordSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(scope))!;
Check(pack.SelectedJobs(savedScope).Select(j => j.JobId).SequenceEqual(pack.SelectedJobs(scope).Select(j => j.JobId)), "Saved class selection must round-trip.");
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
        Check(resolved.Slots.Length <= 20, $"Layout overflow: {job.Job}/{mask}");
        Check(resolved.Slots.Select(s => (s.RegularBar, s.RegularSlot)).Distinct().Count() == resolved.Slots.Length, $"Layout overlap: {job.Job}/{mask}");
        Check(resolved.Slots.All(s => (KeyboardLayout.Positions.Contains(new(s.RegularBar - 1, s.RegularSlot - 1)) || (s.LogicalSlot >= 13 && new Position(s.RegularBar - 1, s.RegularSlot - 1) == ExtraJobMappings.For(job.JobId,s.LogicalSlot).Regular))), $"Unknown layout position: {job.Job}/{mask}");
        if (job.JobId is >= 8 and <= 18) Check(resolved.Slots.Length == 12, "Noncombat page must remain complete.");
        if (job.Job == "DNC")
        {
            Check(ids.Contains(4018) && ids.Contains(4045), "DNC completion must survive manual variants.");
            Check(resolved.Slots.Any(s => s.Action == "Technical Step") == !settings.Enabled(VariantId.ClassMechanics) && resolved.Slots.Any(s => s.Action == "Standard Step") == !settings.Enabled(VariantId.ClassMechanics), "Dance initiation/prepull access disagrees with mechanics.");
        }
        if (job.Job == "AST") Check(resolved.Slots.Any(s => s.Action == "Astral Draw") == !settings.Enabled(VariantId.ClassMechanics) && resolved.Slots.Any(s => s.Action == "Divination") == !settings.Enabled(VariantId.Burst), "AST initiators disagree with their independent variants.");
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

// Every protection transition must remove general selectors while keeping distinct decisions.
var distinctProtection = new Dictionary<uint, string[]>
{
    [19] = ["Clemency", "Passage of Arms", "Arm's Length", "Cover", "Sheltron", "Hallowed Ground"],
    [21] = ["Arm's Length", "Equilibrium", "Raw Intuition", "Holmgang"],
    [32] = ["Arm's Length", "Oblation", "The Blackest Night", "Living Dead"],
    [37] = ["Arm's Length", "Aurora", "Heart of Stone", "Superbolide"],
    [24] = ["Asylum", "Benediction"], [28] = ["Sacred Soil", "Deployment Tactics", "Expedient"],
    [33] = ["Collective Unconscious"], [40] = ["Panhaima", "Pneuma"],
    [27] = ["Radiant Aegis"], [42] = ["Tempera Coat"],
};
foreach (var job in pack.Data.Jobs.Where(j => j.JobId >= 19))
    for (int mask = 0; mask < 16; mask++)
    {
        var settings = new AccordSettings();
        foreach (var variant in VariantDefinitions.All) settings.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
        var resolved = pack.Resolve(job, settings);
        bool automatic = settings.Enabled(VariantId.Protection);
        var ids = resolved.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).ToHashSet();
        if(job.JobId is 19 or 21 or 32 or 37 or 28 or 40)
            foreach(var action in distinctProtection.GetValueOrDefault(job.JobId,[]))
                Check(resolved.Slots.Any(s=>s.Action==action),"Manual protection removed a distinct intent: "+job.Job+"/"+action);
        if (automatic)
        {
            Check(!ids.Overlaps(SetupPack.ManualProtectionPresets(job.JobId)), "A general protection selector remains enabled: " + job.Job);
            foreach (var action in distinctProtection.GetValueOrDefault(job.JobId, []))
                Check(resolved.Slots.Any(s => s.Action == action), "Lost distinct intent: " + job.Job + "/" + action);
            if (job.JobId is not (19 or 21 or 32 or 37 or 24 or 28 or 33 or 40))
            {
                var slot = resolved.Slots.Single(s => s.LogicalSlot == 5);
                Check(slot.Action == "Empty" && slot.RetiredAction == job.Slots.Single(s => s.LogicalSlot == 5).Action,
                    "Automated defense retains its general button or loses safe removal identity: " + job.Job);
            }
        }
        else Check(resolved.Slots.Any(s => s.Action == job.Slots.Single(s => s.LogicalSlot == 5).Action),
            "Manual protection failed to restore its selector: " + job.Job);
    }
var extraMaps = ExtraJobMappings.Merge(Defaults.Maps(), [ExtraJobMappings.For(19), ExtraJobMappings.For(1)], new());
Check(extraMaps.Count == Defaults.Maps().Count + 2, "Extra job mappings were not added.");
Check(extraMaps.Where(m => m.JobId != 0).All(m => !m.Applies(37)), "Paladin overflow leaked into another class.");
Check(ExtraJobMappings.Merge(extraMaps, [ExtraJobMappings.For(19)], new()).Count == extraMaps.Count, "Extra mapping addition is not idempotent.");
var collision = Defaults.Maps();
collision.Add(new SlotMap { Id = "custom", Profile = "All", RegularBar = 3, CrossSet = 2 });
try { ExtraJobMappings.Merge(collision, [ExtraJobMappings.For(19)], new()); Check(false, "Custom mapping collision accepted."); }
catch (InvalidDataException) { Check(collision.Count == Defaults.Maps().Count + 1, "Rejected extension mutated the map."); }
try { ExtraJobMappings.Merge(Defaults.Maps(), [ExtraJobMappings.For(8)], new()); Check(false, "Noncombat extra job accepted."); }
catch (InvalidDataException) { }

// All four variants: explicit initiation ownership and retained, distinct controls.
var burstSlots = new Dictionary<uint, int> { [19]=3,[21]=3,[32]=3,[20]=3,[22]=3,[34]=3,[39]=3,[41]=3,[23]=3,[31]=3,[27]=3,[35]=3,[42]=3,[37]=3,[24]=11,[28]=11,[33]=12,[38]=12 };
var mechanicSlots = new Dictionary<uint, int> { [20]=6,[22]=6,[30]=8,[34]=6,[39]=6,[41]=6,[23]=11,[33]=11,[38]=3,[31]=6 };
var retainedUtility = new Dictionary<uint, string[]>
{
    [19]=["Cover","Sheltron"], [21]=["Raw Intuition"], [32]=["The Blackest Night","Interject"], [37]=["Heart of Stone"],
    [24]=["Cure","Medica","Swiftcast","Liturgy of the Bell","Benediction"],
    [28]=["Physick","Succor","Swiftcast","Dissipation","Expedient"],
    [33]=["Benefic","Helios","Swiftcast","Synastry","Macrocosmos"],
    [40]=["Diagnosis","Prognosis","Swiftcast","Kardia","Pneuma"],
    [20]=["Thunderclap","Riddle of Earth"], [22]=["Winged Glide","Elusive Jump"],
    [30]=["Hide","Kassatsu","Shukuchi","Meisui"], [34]=["Hissatsu: Gyoten","Meditate"],
    [39]=["Hell's Ingress","Soulsow"], [41]=["Slither","Second Wind"],
    [23]=["Peloton","The Warden's Paean","Repelling Shot"], [31]=["Peloton"],
    [38]=["Peloton","Curing Waltz","En Avant"],
    [25]=["Ley Lines","Aetherial Manipulation","Transpose"],
    [27]=["Astral Flow","Swiftcast","Lux Solaris","Radiant Aegis"],
    [35]=["Vercure","Swiftcast","Displacement"], [42]=["Tempera Coat","Hammer Stamp","Smudge"],
};
Check(retainedUtility.Count == 21, "Utility review must cover every combat job.");
foreach (var job in pack.Data.Jobs)
    for (int mask=0; mask<16; mask++)
    {
        var choices=new AccordSettings();
        foreach(var v in VariantDefinitions.All) choices.Variants[v.Id]=(mask & (1 << (int)v.Id)) != 0;
        var resolved=pack.Resolve(job,choices);
        var ids=resolved.Settings["EnabledActionsV6"]!.AsArray().Select(n=>n!.GetValue<int>()).ToHashSet();
        foreach(int id in SetupPack.SupportPresets(job.JobId))
            Check(ids.Contains(id)==choices.Enabled(VariantId.Support), $"Support ownership mismatch: {job.Job}/{id}");
        if(burstSlots.TryGetValue(job.JobId,out int burstSlot))
            Check((resolved.Slots.Single(s=>s.LogicalSlot==burstSlot).Action=="Empty")==choices.Enabled(VariantId.Burst), $"Burst button mismatch: {job.Job}/{mask}");
        if(mechanicSlots.TryGetValue(job.JobId,out int mechanicSlot))
            Check((resolved.Slots.Single(s=>s.LogicalSlot==mechanicSlot).Action=="Empty")==choices.Enabled(VariantId.ClassMechanics), $"Mechanic button mismatch: {job.Job}/{mask}");
        if(job.JobId==31) Check(ids.Contains(8305)==choices.Enabled(VariantId.ClassMechanics),"Flamethrower automation and its manual initiator must have the same owner.");
        if(job.JobId==34) Check(resolved.Slots.Any(s=>s.Action=="Third Eye") != choices.Enabled(VariantId.Protection),"Third Eye button must follow protection.");
        if(job.JobId==32) Check(!ids.Contains(5121),"Burst selector must not consume an interrupt request.");
        if(job.JobId==30) Check(!ids.Contains(10036) && !ids.Contains(10040),"TCJ initiation must not turn into unrelated Meisui spending.");
        if(job.JobId is 20 or 23)
            Check((resolved.Slots.Single(s=>s.LogicalSlot==(job.JobId==20?9:6)).Action=="Empty")==choices.Enabled(VariantId.Support),"Group healing boost button mismatches Support.");
        if(job.JobId==38)
        {
            Check(!ids.Contains(4175),"Manual partner control must not override the chosen ally.");
            foreach (string action in new[] { "Improvisation", "Closed Position" })
                Check(resolved.Slots.Any(s => s.Action == action) == !choices.Enabled(VariantId.Support), "DNC support initiation has a duplicate or missing button: " + action);
            Check(new[] { 4022, 4049 }.All(ids.Contains) == choices.Enabled(VariantId.Support), "Improvisation is missing an automatic damage route.");
            Check(new[] { 4015, 4016, 4018, 4043, 4044, 4045 }.All(ids.Contains), "Manual dances lost step/finish handling.");
            if (mask == 15) Check(resolved.Slots.Count(s => s.Action != "Empty") == 5, "All-on DNC should have five distinct controls.");
        }
        foreach(string action in retainedUtility.GetValueOrDefault(job.JobId,[]))
            Check(resolved.Slots.Any(s=>s.Action==action),$"Lost distinct utility: {job.Job}/{action}/{mask}");
        foreach(var slot in resolved.Slots.Where(s=>s.Action=="Empty"))
        {
            var baseline=job.Slots.SingleOrDefault(s=>s.LogicalSlot==slot.LogicalSlot);
            if(baseline is not null && baseline.Action!="Empty")
                Check(slot.RetiredAction==baseline.Action,$"Retired button cannot be safely cleared: {job.Job}/{slot.LogicalSlot}");
        }
        var features=SetupPack.EngineFeatures(job.JobId,choices);
        if(job.JobId==20)
        {
            Check(features[Feature.MnkMantraOnAoe]==choices.Enabled(VariantId.Support),"Mantra AoE ownership mismatch.");
            Check(features[Feature.MnkEarthOnAoe]==choices.Enabled(VariantId.Protection),"Riddle of Earth must follow Protection on AoE.");
            Check(features[Feature.MnkEarthReplyOnDamage]==choices.Enabled(VariantId.Support),"Earth's Reply must follow Support independently of Riddle initiation.");
        }
        if(job.JobId==34) Check(features[Feature.SamThirdEyeOnAoe]==choices.Enabled(VariantId.Protection),"Third Eye must follow Protection on AoE.");
        if(job.JobId==31) Check(features[Feature.MchFlamethrowerContinuation],"A manually started Flamethrower must retain channel protection.");
        if(job.JobId==31) Check(features[Feature.MchWildfireOnAoe]==choices.Enabled(VariantId.Burst),"Wildfire AoE ownership mismatch.");
        if(job.JobId==38)
        {
            Check(features[Feature.DncPartnerOnAoe]==choices.Enabled(VariantId.Support),"Partner AoE ownership mismatch.");
            Check(!features[Feature.DncTargetlessPrepull],"Retired targetless initiation must remain disabled for every variant.");
            Check(features[Feature.DncImprovisationFinish],"An accepted Improvisation must remain finishable after switching Support off.");
            foreach (int slot in new[] {7,9,11})
                if (resolved.Slots.Single(s => s.LogicalSlot == slot) is { Action: "Empty" } removed)
                    Check(!string.IsNullOrEmpty(removed.RetiredAction), "DNC duplicate cannot be retired with cleanup off.");
        }
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
        if (job.JobId == 25) Check(features[Feature.BlmManawardOnAoe] == settings.Enabled(VariantId.Protection), "Manaward AoE must follow protection.");
        if (job.JobId == 24) Check(features[Feature.WhmDivineCaressOnDamage] == settings.Enabled(VariantId.Protection), "Divine Caress follow-through must follow protection.");
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
// An idle live check must not write or create backups; slot or route drift must repair.
var desiredRoutes = new Dictionary<uint, Dictionary<string, Position>> { [38] = new() { ["st"] = new(0, 0) } };
var existingRoutes = new Dictionary<uint, Dictionary<string, Position>> { [38] = new() { ["st"] = new(0, 0) }, [31] = new() };
Check(!HotbarRefreshPolicy.NeedsApply(0, desiredRoutes, existingRoutes), "Unchanged layout or another job caused an idle rewrite.");
Check(HotbarRefreshPolicy.NeedsApply(1, desiredRoutes, existingRoutes), "A new leveling action was not detected as a change.");
existingRoutes[38]["st"] = new(0, 1);
Check(HotbarRefreshPolicy.NeedsApply(0, desiredRoutes, existingRoutes), "A changed Bridge route was missed.");
existingRoutes.Remove(38);
Check(HotbarRefreshPolicy.NeedsApply(0, desiredRoutes, existingRoutes), "Missing Bridge routes were missed.");

// Class transitions bypass idle polling; failures remain pending without a variant click.
var liveUpdates = new LiveUpdateState();
Check(liveUpdates.Poll(1000, 38, 1), "First live update was delayed.");
try { liveUpdates.Apply(38, 1, () => throw new IOException("Hotbars not ready")); } catch (IOException) { }
liveUpdates.Wake();
liveUpdates.DeferRetry(1000);
Check(!liveUpdates.Applied && !liveUpdates.Poll(1249, 38, 1) && liveUpdates.Poll(1250, 38, 1), "Failed live update did not retry with bounded delay.");
liveUpdates.Apply(38, 1, () => { });
Check(liveUpdates.Applied, "Successful live update remained pending.");
Check(liveUpdates.Poll(1500, 38, 1) && !liveUpdates.Poll(2499, 38, 1) && liveUpdates.Poll(2500, 38, 1), "Settled live checks must recur once per second.");
Check(liveUpdates.Poll(1251, 31, 1) && !liveUpdates.Applied, "Class change waited for idle poll.");
liveUpdates.Apply(31, 1, () => { });
Check(liveUpdates.Poll(1252, 31, 2) && !liveUpdates.Applied, "Character change did not refresh layout.");
liveUpdates.Apply(31, 2, () => { });
liveUpdates.Request();
Check(liveUpdates.Poll(1253, 31, 2) && !liveUpdates.Applied, "Variant or cleanup change did not refresh current layout.");
checks += HotbarIndependenceTests.Run();
checks += ManualFallbackTests.Run();
checks += UpstreamMigrationTests.Run();
checks += SeparationTests.Run();
if (args.Length > 0) checks += EngineSchema.Verify(args[0], pack);
Console.WriteLine($"Passed {checks} assertions across 512 PvE job/variant combinations {(args.Length > 0 ? "plus 336 PvP combinations with engine-schema verification" : "PvP engine-schema checks not run: supply the engine DLL")}, burst handoff, shared-slot cleanup and transaction failure recovery.");
