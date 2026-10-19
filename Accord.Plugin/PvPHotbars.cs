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
    internal string Notice { get; private set; } = "";
    internal sealed class Backup
    {
        public bool PvP { get; set; } = true;
        public string Character { get; set; } = "";
        public List<SlotEdit> Edits { get; set; } = new();
    }
    internal static bool Initialized(uint job) => HotbarLayoutStore.Initialized(job);

    internal unsafe string? Apply(PvPProfile[] jobs, PvPSettings settings, JObject snapshot, bool inspect = false)
    {
        uint current=(uint)Player.Job;
        NativeHotbars.Ready(current,true);
        if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
            throw new InvalidOperationException("PvP hotbar changes wait for combat and loading to finish.");
        HotbarLayoutStore.Ready(true);
        var protectedSlots=HotbarLayoutStore.ProtectedSlots();
        var maps=Defaults.Maps().Where(m=>m.Profile!="Noncombat").ToList();
        bool Protected(Position p) => protectedSlots.Contains(p) || NativeHotbars.Ready(current,true)->IsHotbarShared((uint)p.Bar);
        Notice="";
        var extras=jobs.SelectMany(j=>PvPLayout.AvailableExtras(j,maps,Protected)).ToArray();
        foreach(var extra in extras)if(maps.All(m=>m.Id!=extra.Id))maps.Add(extra);
        Defaults.Validate(maps);
        var backup=new Backup { Character=NativeHotbars.CharacterKey(Svc.PlayerState.ContentId) };
        foreach (var job in jobs)
        {
            // Do not create a never-visited job's saved PvP page before the game initializes it.
            if (job.JobId!=current && !Initialized(job.JobId)) throw new InvalidOperationException("Visit PvP on "+job.Job+" before writing its saved layout.");
            var pairs=PvPLayout.Pairs(job,maps);
            var values=job.Controls.Select(c=>PvPPack.Resolve(c,settings,snapshot)).ToArray();
            bool existingSprint=ModeActions.HasSharedSprint(_=>true, p=>NativeHotbars.Read(job.JobId,p,pvp:true));
            if(existingSprint)
                for(int i=0;i<values.Length;i++)if(job.Controls[i].Binding is { Type:1, Id:29057 })values[i]=default;
            foreach (var control in job.Controls) ValidateBinding(control.Binding);
            var route=PvPLayout.IntentRoutes(job,pairs,values,Protected);
            var layoutRoute=route.Where(r=>maps.Any(m=>m.Id==r.Key)).ToDictionary(r=>r.Key,r=>r.Value);
            LayoutRoutes.Effective(maps.Where(m=>m.Applies(job.JobId)),layoutRoute);
            var keep=new HashSet<Position>();
            var design=job.Controls.Select(c=>c.Binding).Concat(job.RetiredBindings).Append(new SlotValue(1,1_000_000)).ToHashSet();
            for(int i=0;i<pairs.Length;i++)
            {
                if(route.TryGetValue(pairs[i].Id,out var destination))Add(destination,values[i]);
                if(maps.Any(m=>m.Id==pairs[i].Id) && !Protected(pairs[i].Cross))Add(pairs[i].Cross,values[i]);
                else if(values[i]!=default)Notice=" Some controls are keyboard-only because their controller slots are shared or unavailable. Shared bars were preserved.";
            }
            if(settings.ClearOtherActions)
                for(int bar=0;bar<18;bar++)
                    for(int slot=0;slot<(bar<10?12:16);slot++)
                    {
                        var p=new Position(bar,slot);
                        if(!keep.Contains(p) && !Protected(p) && !ModeActions.IsSprint(NativeHotbars.Read(job.JobId,p,pvp:true))) Add(p,default);
                    }
            foreach(var p in Enumerable.Range(0,2).SelectMany(b=>Enumerable.Range(0,12).Select(s=>new Position(b,s))))
                if(!keep.Contains(p) && !Protected(p) && design.Contains(NativeHotbars.Read(job.JobId,p,pvp:true)))Add(p,default);
            void Add(Position p,SlotValue value)
            {
                if(!keep.Add(p) || !p.Valid)throw new InvalidDataException("Duplicate or invalid PvP destination.");
                if(Protected(p))throw new InvalidOperationException($"{p} is shared or linked across modes; PvP setup preserves it. Choose job-specific controller pairs.");
                var before=NativeHotbars.Read(job.JobId,p,pvp:true);
                if(value==default && ModeActions.IsSprint(before))return;
                if(value==default && !settings.ClearOtherActions && !design.Contains(before))return;
                if(before!=value)backup.Edits.Add(new(job.JobId,p,before,value));
            }
        }
        bool changed=backup.Edits.Count>0;
        if(inspect)return changed?"refresh":null;
        return Commit(backup);
    }

    private static void ValidateBinding(SlotValue value)
    {
        if(value==default || value.Type==1 && value.Id is 1_000_000 or 1_000_001 or >=1_000_010 and <=1_000_014)return;
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

    private static unsafe string Commit(Backup backup)
    {
        uint current=(uint)Player.Job;
        HotbarLayoutStore.Ready(true);
        if(!backup.PvP || backup.Character!=NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))throw new InvalidOperationException("Wrong mode or character for this backup.");
        var protectedSlots=HotbarLayoutStore.ProtectedSlots();
        if(backup.Edits.Select(e=>(e.Job,e.Position)).Distinct().Count()!=backup.Edits.Count)throw new InvalidDataException("Duplicate backup destinations.");
        foreach(var e in backup.Edits)
        {
            if(e.Job!=current && !Initialized(e.Job))throw new InvalidOperationException("Visit PvP on this class before restoring its saved layout.");
            if(!e.Position.Valid || protectedSlots.Contains(e.Position) || NativeHotbars.Ready(current,true)->IsHotbarShared((uint)e.Position.Bar))throw new InvalidOperationException("A backup destination is now shared or linked; restore blocked.");
            if(NativeHotbars.Read(e.Job,e.Position,pvp:true)!=e.Before)throw new InvalidOperationException("PvP hotbars changed during setup.");
        }
        var path=Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(),"hotbar-backups-pvp",backup.Character,$"{DateTime.UtcNow:yyyyMMdd-HHmmss-ffff}-{Guid.NewGuid():N}.json");
        JsonStore.Write(path,backup);
        try
        {
            HotbarTransaction.Apply(backup.Edits,(e,undo)=>NativeHotbars.Write(current,e.Job,e.Position,undo?e.Before:e.After,pvp:true),
                null,null);
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
        HotbarLayoutStore.Ready(true);
        if(backup.Character!=NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))throw new InvalidOperationException("This backup belongs to another character.");
        if(backup.Edits.Any(e=>e.Job!=(uint)Player.Job && !Initialized(e.Job)))
            throw new InvalidOperationException("Visit PvP on the backed-up classes before restoring their saved layouts.");
        return Commit(new Backup {Character=backup.Character,
            Edits=backup.Edits.Select(e=>new SlotEdit(e.Job,e.Position,NativeHotbars.Read(e.Job,e.Position,pvp:true),e.Before)).ToList()});
    }
}
