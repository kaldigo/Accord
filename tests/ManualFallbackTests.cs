using System.Text.Json;
using Accord;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;

internal static class ManualFallbackTests
{
    internal static int Run()
    {
        int count = 0;
        void Check(bool value, string message) { count++; if (!value) throw new Exception("Manual fallback: " + message); }
        void Reject(Action action, string message) { bool rejected = false; try { action(); } catch { rejected = true; } Check(rejected,message); }
        var originalRoot = Svc.PluginInterface.Root;
        var root = Path.Combine(Path.GetTempPath(), "accord-manual-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Svc.PluginInterface.Root = root;
        try
        {
            var pack = new ManualLayoutPack();
            var normalPack = new SetupPack();
            Check(pack.Jobs.Select(j => j.JobId).Order().SequenceEqual(normalPack.Data.Jobs.Where(j => j.JobId >= 19).Select(j => j.JobId).Order()), "combat job coverage differs from Accord selection");
            var rolePositions = new Dictionary<string, Position>();
            foreach (var job in pack.Jobs)
            {
                var layout = ManualLayoutPack.Create(job);
                Check(layout.Count == 84 && layout.Keys.All(ManualLayoutPack.Positions.Contains), job.Job + " escaped three regular/controller bars");
                Check(layout.Count(e => e.Key.Bar < 10 && e.Value != "Empty") <= 36, job.Job + " exceeds three bars");
                Check(layout[new(2,10)] == "Limit Break" && layout[new(2,11)] == "Sprint", "utilities moved between jobs");
                foreach (var role in job.RoleActions)
                {
                    var position = layout.Single(e => e.Key.Bar < 10 && e.Value == role).Key;
                    if (rolePositions.TryGetValue(job.Role + role, out var old)) Check(old == position, "role positions drift across jobs");
                    rolePositions[job.Role + role] = position;
                }
                Check(!job.Actions.Any(n => n.Contains("SimpleMode") || n.Contains("AdvancedMode")), "engine-only button in manual layout");
                Check(job.Actions.Concat(job.RoleActions).All(n => layout.Count(e => e.Key.Bar < 10 && e.Value == n) == 1), job.Job + " missing or duplicate manual action");
            }
            Check(pack.Jobs.Single(j=>j.Job=="SCH").Actions.Contains("Summon Eos"), "fairy cannot be resummoned after death");
            Check(pack.Jobs.Single(j=>j.Job=="PLD").Actions.Contains("Goring Blade") && pack.Jobs.Single(j=>j.Job=="SCH").Actions.Contains("Consolation"), "optional native replacement disabled would strand a follow-up");
            var fallback = new ManualFallback();
            var positions = ManualLayoutPack.Positions;
            var targets = new uint[] { 19, 1, 28, 26 };
            void Seed()
            {
                Player.Job = 19; Svc.PlayerState.ContentId = 123; Svc.ClientState.IsLoggedIn = true;
                Svc.Condition.Blocked = false; NativeHotbars.Shared.Clear(); NativeHotbars.Mode = false;
                NativeHotbars.Slots.Clear(); NativeHotbars.FailAt = 0;
                foreach (uint job in targets.Concat([24u]))
                    foreach (var p in positions) NativeHotbars.Put(job,p,new(7,1000+job*100+(uint)(p.Bar*16+p.Slot)));
                NativeHotbars.Put(19,new(9,1),new(10,4)); // Unrelated utility/shared data stays untouched.
                NativeHotbars.Put(19,new(17,3),new(7,999));
                NativeHotbars.Put(19,new(0,0),new(1,4444),true);
            }
            Seed();
            var before = NativeHotbars.Slots.ToDictionary();
            fallback.Prepare([19,28],exiting:true);
            Check(fallback.Pending && fallback.Holding && fallback.ExitPending,"logout does not suspend normal live updates");
            Check(NativeHotbars.Flushes > 0,"native file save was not requested");
            Check(NativeHotbars.Read(19,new(0,8)) != before[(false,19,new(0,8))],"old Bridge-protected slots still excluded");
            Check(NativeHotbars.Read(19,new(2,10)) == new SlotValue(10,3),"Limit Break assigned with the wrong native command type");
            Check(NativeHotbars.Read(19,new(9,1)) == new SlotValue(10,4) && NativeHotbars.Read(19,new(17,3)) == new SlotValue(7,999),"utility bars changed");
            Check(NativeHotbars.Read(24,new(0,0)) == before[(false,24,new(0,0))],"unselected job changed");
            Check(NativeHotbars.Read(19,new(0,0),live:false,pvp:true) == new SlotValue(1,4444),"PvP hotbars changed");
            string journal = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(),"manual-fallback","123","pending.json");
            string original = File.ReadAllText(journal);
            fallback.Prepare([19,28],exiting:true);
            Check(File.ReadAllText(journal)==original,"second logout replaced the normal snapshot with manual bars");
            fallback = new ManualFallback(); // New process/plugin instance, no dependence on Wrath.
            Check(fallback.Pending && !fallback.Holding,"saved fallback not detected after reload");
            Svc.PlayerState.ContentId=456;
            Check(!fallback.Pending,"another character consumed this snapshot");
            Svc.PlayerState.ContentId=123;
            NativeHotbars.Put(19,new(0,0),new(7,4321));
            int writes = NativeHotbars.Writes;
            Reject(()=>fallback.Restore([19,28]),"outside edits silently overwritten");
            Check(NativeHotbars.Writes==writes && File.Exists(journal),"conflict partially restored or lost recovery record");
            var saved = JsonSerializer.Deserialize<ManualFallbackRecord>(original)!;
            NativeHotbars.Put(19,new(0,0),saved.Slots.Single(s=>s.Job==19 && s.Position==new Position(0,0)).After);
            NativeHotbars.Shared.Add(2);
            Reject(()=>fallback.Restore([19,28]),"restore overwrote a newly shared bar");
            NativeHotbars.Shared.Clear();
            fallback.Restore([19,28]);
            Check(!fallback.Pending,"restore did not retire pending marker");
            Check(before.All(e=>NativeHotbars.Slots.GetValueOrDefault(e.Key)==e.Value),"exact original assignments not restored (including third bar/base classes)");
            // Inject a native failure after mutation. Rollback preserves every old slot;
            // the preparing record allows retry even after a process interruption.
            Seed(); before=NativeHotbars.Slots.ToDictionary();
            NativeHotbars.FailAt=NativeHotbars.Writes+5;
            Reject(()=>fallback.Prepare([19,28]),"native failure accepted");
            Check(fallback.Pending && before.All(e=>NativeHotbars.Slots.GetValueOrDefault(e.Key)==e.Value),"failed preparation did not roll back or retain recovery");
            NativeHotbars.FailAt=0;
            // Logging out after a failed preparation must retry the manual writes,
            // rather than treating the rolled-back compressed bars as a completed fallback.
            fallback.Prepare([19,28],exiting:true);
            Check(NativeHotbars.Read(19,new(0,0))!=before[(false,19,new(0,0))],"retry after failed preparation left compressed bars");
            var retryRecord=JsonSerializer.Deserialize<ManualFallbackRecord>(File.ReadAllText(journal))!;
            Check(retryRecord.Phase=="Manual" && retryRecord.Slots.Single(s=>s.Job==19 && s.Position==new Position(0,0)).Before==before[(false,19,new(0,0))],"retry replaced the original backup");
            fallback.Login();
            fallback.Restore([19,28]);
            // A native PvP context must only alter saved PvE assignments, not live PvP.
            Seed(); NativeHotbars.Mode=true;
            before=NativeHotbars.Slots.ToDictionary();
            fallback.Prepare([19]);
            Check(NativeHotbars.Read(19,new(0,0),pvp:true)==new SlotValue(1,4444),"PvP live action replaced by manual PvE");
            fallback.Login(); fallback.Restore([19]);
            Check(before.All(e=>NativeHotbars.Slots.GetValueOrDefault(e.Key)==e.Value),"PvP-context restore changed source data");
            NativeHotbars.Mode=false;
            // Preflight all jobs/slots before the first write.
            NativeHotbars.Shared.Add(1);writes=NativeHotbars.Writes;
            Reject(()=>fallback.Prepare([19]),"native game-shared bar accepted");
            Check(NativeHotbars.Writes==writes && !fallback.Pending,"shared-bar rejection changed state");
            NativeHotbars.Shared.Clear();
            // Restore failures also roll back, keep the record, and can resume after reload.
            fallback.Prepare([19,28]);
            var manual = NativeHotbars.Slots.ToDictionary();
            NativeHotbars.FailAt=NativeHotbars.Writes+4;
            Reject(()=>fallback.Restore([19,28]),"restore failure accepted");
            Check(fallback.Pending && manual.All(e=>NativeHotbars.Slots.GetValueOrDefault(e.Key)==e.Value),"failed restore lost manual state or recovery record");
            NativeHotbars.FailAt=0;fallback=new ManualFallback();fallback.Restore([19,28]);
            // A damaged/mismatched record blocks writes rather than applying another character's snapshot.
            fallback.Prepare([19]);
            var validJournal=File.ReadAllText(journal);
            var invalid=JsonSerializer.Deserialize<ManualFallbackRecord>(validJournal)!;
            invalid.Character="another-character";JsonStore.Write(journal,invalid);
            writes=NativeHotbars.Writes;
            Reject(()=>fallback.Restore([19]),"mismatched recovery character accepted");
            Check(NativeHotbars.Writes==writes,"invalid journal caused native writes");
            File.WriteAllText(journal,validJournal);fallback.Restore([19]);
            fallback.Prepare([19,28]);
            var unselectedValue=NativeHotbars.Read(28,new(0,0));
            fallback.Login();fallback.Restore([19]);
            Check(NativeHotbars.Read(28,new(0,0))==unselectedValue,"deselected job was rewritten on restoration");
            Check(Directory.GetFiles(Path.GetDirectoryName(journal)!,"original-*.json").Length>=4,"immutable backups were overwritten");
        }
        finally
        {
            NativeHotbars.Mode=false;NativeHotbars.Shared.Clear();NativeHotbars.Slots.Clear();NativeHotbars.FailAt=0;
            Player.Job=19;Svc.PlayerState.ContentId=123;Svc.PluginInterface.Root=originalRoot;
        }
        return count;
    }
}
