using System;

namespace WrathCombo.Engine;

/// <summary>A bounded handoff after a confirmed action effect, never an action queue.</summary>
internal sealed class IntentHandoff
{
    private uint job;
    private long expires;
    private uint pendingJob;
    private uint pendingAction;
    private long pendingUntil;
    internal bool Active(uint currentJob, long now) => job == currentJob && now < expires;
    internal void Accept(uint currentJob, long now)
    {
        // Follow-up buffs do not prolong the same window indefinitely.
        if (Active(currentJob, now)) return;
        job = currentJob;
        expires = now + BurstPolicy.Duration(currentJob);
    }
    internal void ObserveInput(uint currentJob, uint action, long now)
    {
        pendingJob = currentJob;
        pendingAction = action;
        pendingUntil = now + 2000;
    }
    internal bool ConfirmInput(uint currentJob, uint action, long now)
    {
        if (pendingJob != currentJob || pendingAction != action || now >= pendingUntil) return false;
        pendingAction = 0;
        Accept(currentJob, now);
        return true;
    }
    internal void Reset() { job = 0; expires = 0; pendingJob = 0; pendingAction = 0; pendingUntil = 0; }
}
