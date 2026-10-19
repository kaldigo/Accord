using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HotbarTools;

public static unsafe class NativeHotbars
{
    public static void Flush(uint currentJob, bool activePvP = false) => Ready(currentJob, activePvP)->SaveFile(true);
    public static RaptureHotbarModule* Ready(uint job,bool pvp=false)
    {
        var m=RaptureHotbarModule.Instance();
        if(m==null || !m->ModuleReady || m->PvPHotbarsActive!=pvp || job==0 || m->ActiveHotbarClassJobId!=job)
            throw new InvalidOperationException("Wait for login/job or hotbar mode switching to finish.");
        return m;
    }
    public static SlotValue Read(uint job,Position p,bool live=true,bool pvp=false)
    {
        if(!p.Valid || job is 0 or >42)throw new InvalidDataException("Invalid hotbar position or job.");
        var m=RaptureHotbarModule.Instance();
        if(m==null || !m->ModuleReady || (live && m->PvPHotbarsActive!=pvp))throw new InvalidOperationException("Hotbars not ready.");
        if(live && m->ActiveHotbarClassJobId==job)
        {
            ref var s=ref m->Hotbars[p.Bar].Slots[p.Slot];
            return new SlotValue((byte)s.CommandType,s.CommandId).Normalized;
        }
        var storage=m->IsHotbarShared((uint)p.Bar)?0:(int)job;
        if(pvp)storage=m->GetPvPSavedHotbarIndexForClassJobId((uint)storage,true);
        if(storage<0)throw new InvalidOperationException("No PvP hotbar storage for this job.");
        ref var saved=ref m->SavedHotbars[storage].Hotbars[p.Bar].Slots[p.Slot];
        return new SlotValue((byte)saved.CommandType,saved.CommandId).Normalized;
    }
    public static void Write(uint currentJob,uint targetJob,Position p,SlotValue value,bool pvp=false,bool? activePvP=null)
    {
        var m=Ready(currentJob,activePvP??pvp);
        if(!p.Valid || targetJob is 0 or >42)throw new InvalidDataException("Invalid hotbar write.");
        value=value.Normalized;
        if(!Enum.IsDefined((RaptureHotbarModule.HotbarSlotType)value.Type))throw new InvalidDataException("Unsupported action type.");
        if(m->PvPHotbarsActive==pvp && (currentJob==targetJob || m->IsHotbarShared((uint)p.Bar)))
            m->SetAndSaveSlot((uint)p.Bar,(uint)p.Slot,(RaptureHotbarModule.HotbarSlotType)value.Type,value.Id,false,pvp);
        else
        {
            // Only command type and ID are consumed by WriteSavedSlot. No live slot is overwritten.
            var source=new RaptureHotbarModule.HotbarSlot { CommandType=(RaptureHotbarModule.HotbarSlotType)value.Type,CommandId=value.Id };
            m->WriteSavedSlot(targetJob,(uint)p.Bar,(uint)p.Slot,&source,false,pvp);
        }
        if(Read(targetJob,p,live:(activePvP??pvp)==pvp,pvp:pvp)!=value)throw new InvalidOperationException($"Hotbar verification failed at job {targetJob} {p}.");
    }
    public static void Apply(uint currentJob,IReadOnlyList<SlotEdit> edits,string backupDirectory,bool pvp=false,bool? activePvP=null)
    {
        Ready(currentJob,activePvP??pvp);
        if(edits.Select(e=>(e.Job,e.Position)).Distinct().Count()!=edits.Count)throw new InvalidDataException("Duplicate write destinations.");
        foreach(var e in edits)
            if(Read(e.Job,e.Position,live:(activePvP??pvp)==pvp,pvp:pvp)!=e.Before)throw new InvalidOperationException("Hotbars changed after preview; preview again.");
        var path=Path.Combine(backupDirectory,$"{DateTime.UtcNow:yyyyMMdd-HHmmss-ffff}-{Guid.NewGuid():N}.json");
        JsonStore.Write(path,edits);
        var applied=new List<SlotEdit>();
        try { foreach(var e in edits) { applied.Add(e); Write(currentJob,e.Job,e.Position,e.After,pvp,activePvP); } }
        catch
        {
            foreach(var e in applied.AsEnumerable().Reverse()) Write(currentJob,e.Job,e.Position,e.Before,pvp,activePvP);
            throw;
        }
    }
    public static List<SlotEdit> RestorePlan(uint currentJob,string path,bool pvp=false)
    {
        Ready(currentJob,pvp);
        var original=System.Text.Json.JsonSerializer.Deserialize<List<SlotEdit>>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid backup");
        return original.Select(e=>new SlotEdit(e.Job,e.Position,Read(e.Job,e.Position,pvp:pvp),e.Before)).ToList();
    }
    public static string CharacterKey(ulong contentId)
    {
        if(contentId==0)throw new InvalidOperationException("Character identity is not ready.");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(BitConverter.GetBytes(contentId)))[..16];
    }
}
