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

    internal static Entry[] Create(uint job, JobPreset preset, IReadOnlyList<SlotMap> maps, Func<Position,bool> protectedSlot)
    {
        var layout=KeyboardLayout.Compile(preset,true);
        var extra=ExtraJobMappings.For(job);
        bool hasExtra=preset.Slots.Any(s=>s.LogicalSlot>12) || maps.Any(m=>m.Id==extra.Id);
        var sources=KeyboardLayout.Positions.Concat(hasExtra ? new[]{extra.Regular} : Array.Empty<Position>()).ToArray();
        var used=maps.Where(m=>m.Applies(job) && m.Id!=extra.Id).Select(m=>layout.GetValueOrDefault(m.Regular,m.Regular))
            .Concat(KeyboardLayout.Positions).ToHashSet();
        var result=new List<Entry>();
        foreach(var source in sources)
        {
            var slot=preset.Slots.SingleOrDefault(s=>new Position(s.RegularBar-1,s.RegularSlot-1)==source);
            var map=maps.SingleOrDefault(m=>m.Applies(job) && m.Regular==source);
            bool registered=map!=null;
            map ??= source==extra.Regular ? extra : throw new InvalidOperationException($"No job-specific Bridge pair for {preset.Job} {source}.");
            if(map.SharedAcrossJobs)throw new InvalidOperationException($"{source} is reserved for shared controls.");
            var regular=layout.GetValueOrDefault(source,source);
            if(source==extra.Regular && protectedSlot(regular))
            {
                regular=Enumerable.Range(0,2).SelectMany(bar=>Enumerable.Range(0,12).Select(s=>new Position(bar,s)))
                    .Where(p=>!used.Contains(p) && !protectedSlot(p)).Select(p=>(Position?)p).FirstOrDefault()
                    ?? throw new InvalidOperationException("No unshared keyboard slot is available for the extra job control.");
            }
            if(protectedSlot(regular))throw new InvalidOperationException($"{regular} is shared or linked; keyboard setup preserves it.");
            used.Add(regular);
            result.Add(new(slot,map,regular,registered && !protectedSlot(map.Cross) ? map.Cross : null,registered));
        }
        if(result.Select(e=>e.Regular).Distinct().Count()!=result.Count)throw new InvalidDataException("Duplicate keyboard destinations.");
        return result.ToArray();
    }
}
