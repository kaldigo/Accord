using System.IO;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;

namespace Accord;

internal static class SprintHotbars
{
    internal static void Refresh(bool pvp)
    {
        HotbarLayoutStore.Ready(pvp);
        uint job = (uint)Player.Job;
        if (pvp) HotbarLayoutStore.ObservePvP(job);
        var edits = ModeActions.SprintEdits(job, pvp, p => NativeHotbars.Read(job, p, pvp: pvp));
        if (edits.Count == 0) return;
        var path = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "sprint-backups",
            NativeHotbars.CharacterKey(Svc.PlayerState.ContentId), pvp ? "pvp" : "pve");
        // Shared utility bars are intentionally included: only known Sprint bindings change.
        NativeHotbars.Apply(job, edits, path, pvp: pvp);
    }
}
