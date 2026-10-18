using System;
using System.Collections.Generic;

namespace WrathCombo.Engine;

internal enum CombatRequest { Offense, Defense, Support, Mechanic }

// A successful press grants a bounded continuation, never a queued game action.
internal sealed class PvPRequestState
{
    private (uint Job, ulong Character) context;
    private (CombatRequest Kind, uint Action, long At)? pending;
    private readonly Dictionary<CombatRequest, long> accepted = new();
    internal void Reset() { context=default; pending=null; accepted.Clear(); }
    internal void Context(uint job, ulong character, bool ready)
    {
        if(!ready || context!=(job,character))Reset();
        if(ready)context=(job,character);
    }
    internal void Observe(CombatRequest kind,uint action,long now)
    {
        if(context.Job!=0 && action is >0 and <1_000_000)pending=(kind,action,now);
    }
    internal void Confirm(uint action,long now)
    {
        if(pending is not {} input || input.Action!=action || now-input.At is <0 or >3000)return;
        // Repeated confirmations and automatic follow-ups never extend the window.
        accepted[input.Kind]=now;
        pending=null;
    }
    internal bool Active(CombatRequest kind,long now) => accepted.TryGetValue(kind,out var start) &&
        now-start>=0 && now-start<(kind==CombatRequest.Mechanic?45000:20000);
    internal void Complete(CombatRequest kind) => accepted.Remove(kind);
}
