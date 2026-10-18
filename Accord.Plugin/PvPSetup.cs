using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accord;

internal sealed class PvPSetup
{
    internal readonly PvPPack Pack = new();
    private readonly EngineClient engine;
    private readonly PvPHotbars hotbars = new();
    private readonly LiveUpdateState updates = new();
    internal PvPSettings Settings => Plugin.Instance.Settings.SharedPvP();
    internal string Status { get; private set; } = "Select the classes Accord should manage in PvP.";
    internal PvPSetup(EngineClient engine) => this.engine=engine;
    internal void Wake() => updates.Wake();
    internal void Invalidate() => updates.Request();
    internal bool Active(uint job) => Settings.ActiveJobs.Contains(job) && Pack.Find(job)!=null;
    private AccordSettings Clone() => JsonConvert.DeserializeObject<AccordSettings>(JsonConvert.SerializeObject(Plugin.Instance.Settings))!;
    private void Save(AccordSettings desired)
    {
        if(engine.HasPending)throw new InvalidOperationException("Resolve the pending settings operation first.");
        Svc.PluginInterface.SavePluginConfig(desired);
        Plugin.Instance.Settings=desired;
    }
    private void Prepare(AccordSettings desired, PvPProfile[] jobs)
    {
        desired.SharedPvP();
        if(Plugin.Instance.MigrationPending)throw new InvalidOperationException("Complete settings migration first.");
        if(jobs.Length==0){Save(desired);return;}
        var snapshot=engine.Read();
        if(desired.EngineConfigurationId!=snapshot.Value<string>("ConfigurationId"))
        {desired.PreparedJobs.Clear();desired.SharedSettingsPrepared=false;}
        if(desired.PvP.ConfigurationId!=snapshot.Value<string>("ConfigurationId"))desired.PvP.PreparedJobs.Clear();
        desired.PvP.ConfigurationId=snapshot.Value<string>("ConfigurationId")!;
        foreach(var job in jobs)desired.PvP.PreparedJobs[job.JobId]=PvPPack.Revision;
        // Only scoped PvP presets/features. AutoActions and every master toggle remain untouched.
        engine.Apply(snapshot,Pack.Patch(jobs,desired.PvP,snapshot),desired);
    }
    private PvPProfile[] Selected(PvPSettings settings) => Pack.Jobs.Where(j=>settings.ActiveJobs.Contains(j.JobId)).ToArray();
    internal void Reset() {Prepare(Clone(),Selected(Settings));Invalidate();Status="PvP intent settings reapplied. Personal thresholds and hands-free settings retained.";}
    internal unsafe void ApplySelected()
    {
        var selected=Selected(Settings);
        if(selected.Length==0)throw new InvalidOperationException("Select at least one PvP class first.");
        NativeHotbars.Ready((uint)Player.Job,true);
        Prepare(Clone(),selected);
        var ready=selected.Where(j=>j.JobId==(uint)Player.Job || PvPHotbars.Initialized(j.JobId)).ToArray();
        var desired=Clone();
        desired.PvP.PendingLayouts.UnionWith(selected.Except(ready).Select(j=>j.JobId));
        Save(desired);
        if(ready.Length>0)Apply(ready,engine.Read());
        Status=$"PvP hotbars applied for {ready.Length} classes. "+(Settings.PendingLayouts.Count>0
            ? $"{Settings.PendingLayouts.Count} new PvP classes queued: each applies once when you visit PvP on that class."
            : "Recovery backup saved.")+hotbars.Notice;
    }
    private void Apply(PvPProfile[] jobs,JObject snapshot)
    {
        var path=hotbars.Apply(jobs,Settings,snapshot)!;
        var desired=Clone();desired.PvP.LastHotbarBackup=path;
        desired.PvP.PendingLayouts.ExceptWith(jobs.Select(j=>j.JobId));
        try{Save(desired);}catch{hotbars.Restore(path);throw;}
    }
    internal void Tick()
    {
        uint job=Player.Available?(uint)Player.Job:0;
        ulong character=Player.Available && Svc.ClientState.IsLoggedIn?Svc.PlayerState.ContentId:0;
        if(!updates.Poll(Environment.TickCount64,job,character))return;
        try
        {
            if(!Active(job)){updates.Invalidate();Status="Accord is inactive for this PvP class.";return;}
            if(character==0)return;
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
            {Status="PvP layout paused while loading.";return;}
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            {Status="PvP layout paused during combat; it will resume afterwards.";return;}
            unsafe{NativeHotbars.Ready(job,true);}
            var profile=Pack.Find(job)!;
            var snapshot=engine.Read();
            if(Settings.ConfigurationId!=snapshot.Value<string>("ConfigurationId"))updates.Invalidate();
            if(!updates.Applied)
                updates.Apply(job,character,()=>{
                    Prepare(Clone(),[profile]);
                    if(Settings.LiveMode || Settings.PendingLayouts.Contains(job))Apply([profile],engine.Read());
                });
            else if(Settings.LiveMode && hotbars.Apply([profile],Settings,snapshot,inspect:true)!=null)
                Apply([profile],snapshot);
            Status=(Settings.LiveMode?"PvP live layout up to date; watching for hotbar changes.":"PvP behaviour ready. Static hotbars retained.")+hotbars.Notice;
        }
        finally{updates.DeferRetry(Environment.TickCount64);}
    }
    internal void Restore()
    {
        if(string.IsNullOrEmpty(Settings.LastHotbarBackup))throw new InvalidOperationException("No PvP hotbar backup yet.");
        var undo=hotbars.Restore(Settings.LastHotbarBackup);
        var desired=Clone();desired.PvP.LastHotbarBackup=undo;desired.LiveMode=false;desired.SharedPvP();desired.PvP.PendingLayouts.Clear();
        try{Save(desired);}catch{hotbars.Restore(undo);throw;}
        Status="Previous PvP layout restored. PvP live mode disabled to retain it.";
    }
}
