using Accord;
using HotbarTools;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Newtonsoft.Json.Linq;
using System.Text.Json;

internal static class HotbarIndependenceTests
{
    internal static int Run()
    {
        int count=0;
        void Check(bool value,string message) { count++; if(!value)throw new Exception("Independent hotbars: "+message); }
        void Reject(Action action,string message)
        {
            bool rejected=false; try { action(); } catch { rejected=true; }
            Check(rejected,message);
        }
        var root=Path.Combine(Path.GetTempPath(),"accord-hotbar-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Svc.PluginInterface.Root=root;
        try
        {
            // A stale or damaged Bridge configuration must have no effect on Accord.
            File.WriteAllText(Path.Combine(root,"HotbarBridge.json"),"not a valid Bridge configuration");
            Check(HotbarLayoutStore.ProtectedSlots().Contains(new(9,7)),"Accord utility protection missing");
            Check(ModeActions.SprintEdits(19,true,_=>new(10,4)).Count==248,"scan did not cover every regular/cross slot");
            foreach(var pvp in new[]{false,true})
            foreach(var sprint in new[]{new SlotValue(1,3),new SlotValue(1,29057),new SlotValue(10,4)})
            {
                NativeHotbars.Mode=pvp;
                NativeHotbars.Slots.Clear();NativeHotbars.Shared.Clear();NativeHotbars.Shared.Add(9);
                foreach(var position in new[]{new Position(9,7),new Position(17,15),new Position(4,8),new Position(1,8)})
                    NativeHotbars.Put(19,position,sprint,pvp);
                NativeHotbars.Put(19,new(9,1),new(10,3),pvp);
                NativeHotbars.Put(19,new(9,2),new(9,4),pvp);
                NativeHotbars.Put(19,new(17,14),new(1,555),pvp);
                var edits=ModeActions.SprintEdits(19,pvp,p=>NativeHotbars.Read(19,p,pvp:pvp));
                Check(edits.All(e=>ModeActions.IsSprint(e.Before)),"scan changed non-Sprint assignments");
                SprintHotbars.Refresh(pvp);
                foreach(var position in new[]{new Position(9,7),new Position(17,15),new Position(4,8),new Position(1,8)})
                    Check(NativeHotbars.Read(19,position,pvp:pvp)==ModeActions.ForMode(sprint,pvp),"Sprint not converted on utility/cross/job bar");
                Check(NativeHotbars.Read(19,new(9,1),pvp:pvp)==new SlotValue(10,3) && NativeHotbars.Read(19,new(9,2),pvp:pvp)==new SlotValue(9,4),"general actions or macros changed");
                Check(ModeActions.SprintEdits(19,pvp,p=>NativeHotbars.Read(19,p,pvp:pvp)).Count==0,"stable Sprint caused repeat writes");
            }
            Check(HotbarLayoutStore.Initialized(19),"current PvP job not recorded");
            Svc.PlayerState.ContentId=456;
            Check(!HotbarLayoutStore.Initialized(19),"initialized PvP job leaked across characters");
            Svc.PlayerState.ContentId=123;
            var pack=new SetupPack();
            var hotbars=new HotbarSetup();
            NativeHotbars.Mode=false;NativeHotbars.Slots.Clear();NativeHotbars.Shared.Clear();
            NativeHotbars.Put(19,new(9,7),new(1,3));
            NativeHotbars.Put(19,new(4,8),new(1,999));
            var choices=new AccordSettings{ActiveJobs=[19]};
            var preset=pack.Resolve(pack.Find(19)!,choices);
            foreach(var cleanup in new[]{false,true})
            {
                var backup=hotbars.Apply([(19,preset)],cleanup);
                Check(NativeHotbars.Read(19,new(9,7))==new SlotValue(1,3),"PvE setup removed utility Sprint");
                Check(NativeHotbars.Read(19,new(4,8))==new SlotValue(1,999),"PvE cleanup cleared protected utility bar");
                Check(NativeHotbars.Read(19,new(10,15))==NativeHotbars.Read(19,new(0,0)),"Accord controller destination was not used");
                Check(!hotbars.NeedsRefresh(19,preset,cleanup),"PvE layout did not settle without Bridge");
                var undo=hotbars.Restore(backup);
                Check(File.Exists(undo),"PvE restore did not save recovery backup");
            }
            NativeHotbars.FailAt=NativeHotbars.Writes+2;
            var before=NativeHotbars.Slots.ToDictionary();
            Reject(()=>hotbars.Apply([(19,preset)],true),"simulated hotbar write failure accepted");
            Check(before.All(e=>NativeHotbars.Slots.GetValueOrDefault(e.Key)==e.Value) && NativeHotbars.Slots.Where(e=>!before.ContainsKey(e.Key)).All(e=>e.Value==default),"failed layout did not roll back");
            NativeHotbars.FailAt=0;
            NativeHotbars.Mode=true;NativeHotbars.Slots.Clear();
            NativeHotbars.Put(19,new(9,7),new(1,29057),true);
            NativeHotbars.Put(19,new(4,8),new(1,999),true);
            var pvpPack=new PvPPack();var pvpHotbars=new PvPHotbars();var profile=pvpPack.Find(19)!;
            var snapshot=new JObject{["Settings"]=new JObject{["CustomActionSettings"]=new JObject()},["PresetIds"]=new JObject()};
            foreach(var cleanup in new[]{false,true})
            {
                var pvpChoices=new PvPSettings{ClearOtherActions=cleanup};
                var backup=pvpHotbars.Apply([profile],pvpChoices,snapshot)!;
                Check(NativeHotbars.Read(19,new(9,7),pvp:true)==new SlotValue(1,29057),"PvP cleanup removed utility Sprint");
                Check(NativeHotbars.Read(19,new(4,8),pvp:true)==new SlotValue(1,999),"PvP cleanup cleared protected utility bar");
                Check(pvpHotbars.Apply([profile],pvpChoices,snapshot,inspect:true)==null,"PvP layout did not settle without Bridge");
                Check(File.Exists(pvpHotbars.Restore(backup)),"PvP restore did not save recovery backup");
            }
            foreach(var job in pack.Data.Jobs)
            foreach(var hasUtilitySprint in new[]{true,false})
            {
                Player.Job=job.JobId;NativeHotbars.Mode=false;NativeHotbars.Slots.Clear();
                if(hasUtilitySprint)NativeHotbars.Put(job.JobId,new(9,7),new(1,3));
                var resolved=pack.Resolve(job,choices);
                hotbars.Apply([(job.JobId,resolved)],true);
                // A fallback Sprint becomes an existing protected control on the next check.
                if(hotbars.NeedsRefresh(job.JobId,resolved,true))hotbars.Apply([(job.JobId,resolved)],true);
                Check(!hotbars.NeedsRefresh(job.JobId,resolved,true),job.Job+" PvE failed to settle with utility/fallback Sprint");
                if(hasUtilitySprint)Check(NativeHotbars.Read(job.JobId,new(9,7))==new SlotValue(1,3),job.Job+" lost utility Sprint");
            }
            foreach(var job in pvpPack.Jobs)
            foreach(var hasUtilitySprint in new[]{true,false})
            {
                Player.Job=job.JobId;NativeHotbars.Mode=true;NativeHotbars.Slots.Clear();
                if(hasUtilitySprint)NativeHotbars.Put(job.JobId,new(9,7),new(1,29057),true);
                var selected=new PvPSettings{ClearOtherActions=true};
                pvpHotbars.Apply([job],selected,snapshot);
                if(pvpHotbars.Apply([job],selected,snapshot,inspect:true)!=null)pvpHotbars.Apply([job],selected,snapshot);
                Check(pvpHotbars.Apply([job],selected,snapshot,inspect:true)==null,job.Job+" PvP failed to settle with utility/fallback Sprint");
                if(hasUtilitySprint)Check(NativeHotbars.Read(job.JobId,new(9,7),pvp:true)==new SlotValue(1,29057),job.Job+" lost utility Sprint");
            }
            Player.Job=19;
            NativeHotbars.Mode=false;
            Svc.Condition.Blocked=true;
            Reject(()=>SprintHotbars.Refresh(false),"Sprint wrote during combat/loading");
            Reject(()=>hotbars.Apply([(19,preset)],true),"PvE setup wrote during combat/loading");
            Svc.Condition.Blocked=false;
            Svc.PluginInterface.InstalledPlugins.Add(new(){InternalName="HotbarBridge",IsLoaded=true});
            Reject(()=>hotbars.Apply([(19,preset)],true),"raced an old active Bridge synchronizer");
            Svc.PluginInterface.InstalledPlugins.Clear();
            HotbarLayoutStore.Reset();
            Check(HotbarLayoutStore.Initialized(19),"observed PvP state did not persist after reload");
        }
        finally
        {
            HotbarLayoutStore.Reset();NativeHotbars.Mode=false;NativeHotbars.Slots.Clear();NativeHotbars.Shared.Clear();
            Svc.Condition.Blocked=false;Svc.PluginInterface.InstalledPlugins.Clear();Svc.PlayerState.ContentId=123;
            var resolved=Path.GetFullPath(root);
            if(!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(resolved).StartsWith("accord-hotbar-tests-",StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected test cleanup path");
            Directory.Delete(resolved,true);
        }
        return count;
    }
}
