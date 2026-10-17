using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;
using Newtonsoft.Json.Linq;
using Sheets = Lumina.Excel.Sheets;

namespace Accord;

internal sealed class PvPHotbars
{
    private static string Read(string name) => Svc.PluginInterface.GetIpcSubscriber<string>("HotbarBridge.PvP."+name).InvokeFunc();
    private static T Read<T>(string name) => JsonSerializer.Deserialize<T>(Read(name)) ?? throw new InvalidDataException("Bridge PvP information unavailable.");
    private static void Write(string name, object value)
    {
        if (!Svc.PluginInterface.GetIpcSubscriber<string,bool>("HotbarBridge.PvP."+name).InvokeFunc(JsonSerializer.Serialize(value)))
            throw new InvalidOperationException("Bridge rejected PvP "+name+".");
    }
    private static void Begin()
    {
        if (!Svc.PluginInterface.InstalledPlugins.Any(p=>p.InternalName=="HotbarBridge" && p.IsLoaded))
            throw new InvalidOperationException("Load the updated Hotbar Bridge for PvP layouts and shared-slot protection.");
        if (!Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.PvP.BeginExternalEdit").InvokeFunc())
            throw new InvalidOperationException("Waiting for Bridge to finish switching to PvP.");
    }
    private static void End() => Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.PvP.EndExternalEdit").InvokeFunc();
    internal sealed class Backup
    {
        public bool PvP { get; set; } = true;
        public string Character { get; set; } = "";
        public List<SlotEdit> Edits { get; set; } = new();
        public Dictionary<uint,Dictionary<string,Position>> Routes { get; set; } = new();
    }
    internal static bool Initialized(uint job) => Read<uint[]>("GetInitializedJobs").Contains(job);

    internal unsafe string? Apply(PvPProfile[] jobs, PvPSettings settings, JObject snapshot, bool inspect = false)
    {
        uint current=(uint)Player.Job;
        NativeHotbars.Ready(current,true);
        if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
            throw new InvalidOperationException("PvP hotbar changes wait for combat and loading to finish.");
        if (!inspect) Begin();
        try
        {
            var protectedSlots=Read<HashSet<Position>>("GetProtectedSlots");
            var maps=Read<List<SlotMap>>("GetMapping");
            var extras=jobs.SelectMany(PvPLayout.Extras).ToArray();
            if (inspect && extras.Any(e=>maps.All(m=>m.Id!=e.Id))) return "refresh";
            if (!inspect && extras.Length>0) { Write("EnsureJobMappings",extras); maps=Read<List<SlotMap>>("GetMapping"); }
            Defaults.Validate(maps);
            var oldRoutes=Read<Dictionary<uint,Dictionary<string,Position>>>("GetJobRoutes");
            var routes=new Dictionary<uint,Dictionary<string,Position>>();
            var backup=new Backup { Character=NativeHotbars.CharacterKey(Svc.PlayerState.ContentId) };
            foreach (var job in jobs)
            {
                // Do not create a never-visited job's saved PvP page before the game initializes it.
                if (job.JobId!=current && !Initialized(job.JobId)) throw new InvalidOperationException("Visit PvP on "+job.Job+" before writing its saved layout.");
                var pairs=job.Controls.Select((_,i)=>maps.SingleOrDefault(m=>m.Applies(job.JobId) && m.Regular==PvPLayout.Positions[i]
                    && !m.SharedAcrossJobs) ?? throw new InvalidOperationException($"No job-specific PvP Bridge pair for {job.Job}, {PvPLayout.Positions[i]}." )).ToArray();
                var values=job.Controls.Select(c=>PvPPack.Resolve(c,settings,snapshot)).ToArray();
                foreach (var control in job.Controls) ValidateBinding(control.Binding);
                var route=PvPLayout.Routes(pairs,values,PvPLayout.Fixed(job));
                LayoutRoutes.Effective(maps.Where(m=>m.Applies(job.JobId)),route);
                routes[job.JobId]=route;
                backup.Routes[job.JobId]=oldRoutes.GetValueOrDefault(job.JobId)??new();
                var keep=new HashSet<Position>();
                var design=job.Controls.Select(c=>c.Binding).Append(new SlotValue(1,1_000_000)).ToHashSet();
                for(int i=0;i<pairs.Length;i++)
                {
                    Add(route[pairs[i].Id],values[i]);
                    Add(pairs[i].Cross,values[i]);
                }
                if(settings.ClearOtherActions)
                    for(int bar=0;bar<18;bar++)
                        for(int slot=0;slot<(bar<10?12:16);slot++)
                        {
                            var p=new Position(bar,slot);
                            if(!keep.Contains(p) && !Protected(p)) Add(p,default);
                        }
                bool Protected(Position p) => protectedSlots.Contains(p) || NativeHotbars.Ready(current,true)->IsHotbarShared((uint)p.Bar);
                void Add(Position p,SlotValue value)
                {
                    if(!keep.Add(p) || !p.Valid)throw new InvalidDataException("Duplicate or invalid PvP destination.");
                    if(Protected(p))throw new InvalidOperationException($"{p} is shared or linked across modes; PvP setup preserves it. Choose job-specific Bridge pairs.");
                    var before=NativeHotbars.Read(job.JobId,p,pvp:true);
                    if(value==default && !settings.ClearOtherActions && !design.Contains(before))return;
                    if(before!=value)backup.Edits.Add(new(job.JobId,p,before,value));
                }
            }
            bool changed=backup.Edits.Count>0 || routes.Any(j=> !oldRoutes.TryGetValue(j.Key,out var old) || old.Count!=j.Value.Count || j.Value.Any(r=>!old.TryGetValue(r.Key,out var p)||p!=r.Value));
            if(inspect)return changed?"refresh":null;
            return Commit(backup,routes);
        }
        finally { if(!inspect)End(); }
    }

    private static void ValidateBinding(SlotValue value)
    {
        if(value.Type==26)
        {
            var row=Svc.Data.GetExcelSheet<Sheets.ActionComboRoute>(ClientLanguage.English).GetRow(value.Id);
            if(row.Name.IsEmpty || !row.Action.Any(a=>a.RowId>0 && a.Value.IsPvP))throw new InvalidDataException("PvP combo sheet changed; update Accord before setup.");
        }
        else
        {
            var row=Svc.Data.GetExcelSheet<Sheets.Action>(ClientLanguage.English).GetRow(value.Id);
            if(!row.IsPvP || !row.IsPlayerAction)throw new InvalidDataException("PvP action sheet changed; update Accord before setup.");
        }
    }

    private static unsafe string Commit(Backup backup, Dictionary<uint,Dictionary<string,Position>> routes)
    {
        uint current=(uint)Player.Job;
        if(!backup.PvP || backup.Character!=NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))throw new InvalidOperationException("Wrong mode or character for this backup.");
        var protectedSlots=Read<HashSet<Position>>("GetProtectedSlots");
        var maps=Read<List<SlotMap>>("GetMapping");
        foreach(var (job,route) in routes)LayoutRoutes.Effective(maps.Where(m=>m.Applies(job)),route);
        if(backup.Edits.Select(e=>(e.Job,e.Position)).Distinct().Count()!=backup.Edits.Count)throw new InvalidDataException("Duplicate backup destinations.");
        foreach(var e in backup.Edits)
        {
            if(!e.Position.Valid || protectedSlots.Contains(e.Position) || NativeHotbars.Ready(current,true)->IsHotbarShared((uint)e.Position.Bar))throw new InvalidOperationException("A backup destination is now shared or linked; restore blocked.");
            if(NativeHotbars.Read(e.Job,e.Position,pvp:true)!=e.Before)throw new InvalidOperationException("PvP hotbars changed during setup.");
        }
        var path=Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(),"hotbar-backups-pvp",backup.Character,$"{DateTime.UtcNow:yyyyMMdd-HHmmss-ffff}-{Guid.NewGuid():N}.json");
        JsonStore.Write(path,backup);
        try
        {
            HotbarTransaction.Apply(backup.Edits,(e,undo)=>NativeHotbars.Write(current,e.Job,e.Position,undo?e.Before:e.After,pvp:true),
                ()=>Write("SetJobRoutes",routes),()=>Write("SetJobRoutes",backup.Routes));
            return path;
        }
        catch(Exception ex) {throw new InvalidOperationException("PvP setup failed. Recovery backup: "+path,ex);}
    }
    internal string Restore(string path)
    {
        // Missing mode tags are legacy PvE backups and must never be accepted here.
        var json=JObject.Parse(File.ReadAllText(path));
        if(json.Value<bool?>("PvP")!=true)throw new InvalidDataException("This is not a PvP backup.");
        var backup=JsonSerializer.Deserialize<Backup>(json.ToString())!;
        Begin();
        try
        {
            var old=Read<Dictionary<uint,Dictionary<string,Position>>>("GetJobRoutes");
            return Commit(new Backup {Character=backup.Character,
                Routes=backup.Routes.Keys.ToDictionary(j=>j,j=>old.GetValueOrDefault(j)??new()),
                Edits=backup.Edits.Select(e=>new SlotEdit(e.Job,e.Position,NativeHotbars.Read(e.Job,e.Position,pvp:true),e.Before)).ToList()},backup.Routes);
        }
        finally{End();}
    }
}
