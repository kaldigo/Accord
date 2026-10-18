using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotbarTools;

namespace Accord;

// Plan keyboard destinations independently of whether a controller mirror can be written.
internal static class PveLayoutPlan
{
    internal sealed record Entry(PresetSlot? Slot, SlotMap Map, Position Regular, Position? Cross, bool Registered);

    internal static Entry[] Create(uint job, JobPreset preset, IReadOnlyList<SlotMap> maps, Func<Position,bool> protectedSlot, bool sprint=false)
    {
        var layout=KeyboardLayout.Compile(preset,true);
        var extras = preset.Slots.Where(s=>s.LogicalSlot>12).Select(s=>ExtraJobMappings.For(job,s.LogicalSlot))
            .Concat(maps.Where(m=>m.Applies(job) && m.Id.StartsWith($"extra-job-{job}-")))
            .DistinctBy(m=>m.Id).ToArray();
        var sources=KeyboardLayout.Positions.Concat(extras.Select(e=>e.Regular)).ToArray();
        var used=maps.Where(m=>m.Applies(job) && !extras.Any(e=>e.Id==m.Id)).Select(m=>layout.GetValueOrDefault(m.Regular,m.Regular))
            .Concat(KeyboardLayout.Positions).ToHashSet();
        var result=new List<Entry>();
        foreach(var source in sources)
        {
            var slot=preset.Slots.SingleOrDefault(s=>new Position(s.RegularBar-1,s.RegularSlot-1)==source);
            var map=maps.SingleOrDefault(m=>m.Applies(job) && m.Regular==source && !m.SharedAcrossJobs);
            bool registered=map!=null;
            map ??= extras.SingleOrDefault(e=>e.Regular==source) ?? new SlotMap{Id=$"pve-keyboard-{job}-{source.Bar}-{source.Slot}",JobId=job,RegularBar=source.Bar+1,RegularSlot=source.Slot+1};
            var regular=layout.GetValueOrDefault(source,source);
            if(extras.Any(e=>e.Regular==source))
            {
                regular=Enumerable.Range(0,12).Select(s=>new Position(1,s))
                    .Where(p=>!used.Contains(p) && !protectedSlot(p)).Select(p=>(Position?)p).FirstOrDefault()
                    ?? throw new InvalidOperationException("No unshared keyboard slot is available for the extra job control.");
            }
            if(protectedSlot(regular))
            {
                if(slot?.LogicalSlot is 1 or 2)throw new InvalidOperationException($"{regular} is shared; ST/AoE need job-specific positions.");
                regular=Enumerable.Range(0,regular.Bar==0?8:12).Select(s=>new Position(regular.Bar,s))
                    .Where(p=>!used.Contains(p) && !protectedSlot(p)).Select(p=>(Position?)p).FirstOrDefault()
                    ?? throw new InvalidOperationException("No unshared position remains on this job bar.");
            }
            used.Add(regular);
            result.Add(new(slot,map,regular,registered && !protectedSlot(map.Cross) ? map.Cross : null,registered));
        }
        if(sprint && Defaults.IsCombat(job))
        {
            var p=Enumerable.Range(0,12).Select(s=>new Position(1,s)).Where(p=>!result.Any(e=>e.Regular==p) && !protectedSlot(p))
                .Select(p=>(Position?)p).FirstOrDefault() ?? throw new InvalidOperationException("No job slot available for Sprint.");
            var slot=new PresetSlot{LogicalSlot=99,Action="Sprint",Function="Travel",RegularBar=2,RegularSlot=p.Slot+1};
            result.Add(new(slot,new SlotMap{Id=$"pve-sprint-{job}",JobId=job,RegularBar=2,RegularSlot=p.Slot+1},p,null,false));
        }
        // Extras are job controls too: include them in the same per-bar compaction,
        // without moving the reserved tank/healer controls or controller endpoints.
        var locked = preset.Slots.Where(s=>s.LogicalSlot is 1 or 2 ||
            ((Defaults.IsHealer(preset.JobId) || preset.JobId is 19 or 21 or 32 or 37) && s.LogicalSlot is 9 or 10) ||
            (Defaults.IsCombat(job) && !Defaults.IsHealer(job) && s.LogicalSlot==4 && s.Action!="Empty"))
            .Select(s=>s.LogicalSlot).ToHashSet();
        foreach(var bar in result.Where(e=>e.Slot is null || !locked.Contains(e.Slot.LogicalSlot)).GroupBy(e=>e.Regular.Bar).ToArray())
        {
            var fixedPositions=result.Where(e=>e.Slot is not null && locked.Contains(e.Slot.LogicalSlot)).Select(e=>e.Regular).ToHashSet();
            var destinations=Enumerable.Range(0,bar.Key==0?8:12).Select(s=>new Position(bar.Key,s))
                .Where(p=>!protectedSlot(p) && !fixedPositions.Contains(p)).ToArray();
            var ordered=bar.OrderBy(e=>e.Slot is null || e.Slot.Action=="Empty" ? 1:0).ThenBy(e=>e.Regular.Slot).ToArray();
            if(ordered.Length>destinations.Length)throw new InvalidOperationException("Not enough unshared job slots.");
            for(int i=0;i<ordered.Length;i++)result[result.IndexOf(ordered[i])]=ordered[i] with { Regular=destinations[i] };
        }
        if(result.Select(e=>e.Regular).Distinct().Count()!=result.Count)throw new InvalidDataException("Duplicate keyboard destinations.");
        return result.ToArray();
    }
}
