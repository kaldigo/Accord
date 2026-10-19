using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;

namespace Accord;

internal static class HotbarLayoutStore
{
    private static Dictionary<string, HashSet<uint>>? observed;
    private static string PathName => Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "pvp-observed-jobs.json");
    private static Dictionary<string, HashSet<uint>> Observed => observed ??=
        File.Exists(PathName) ? JsonSerializer.Deserialize<Dictionary<string, HashSet<uint>>>(File.ReadAllText(PathName))
            ?? throw new InvalidDataException("Invalid PvP initialization record.") : new();
    internal static HashSet<Position> ProtectedSlots() => Defaults.Maps().Where(m => m.SharedAcrossJobs)
        .SelectMany(m => new[] { m.Regular, m.Cross })
        .Concat(Enumerable.Range(2, 8).SelectMany(bar => Enumerable.Range(0, 12).Select(slot => new Position(bar, slot)))).ToHashSet();
    internal static unsafe void Ready(bool pvp)
    {
        if (!Player.Available || !Svc.ClientState.IsLoggedIn) throw new InvalidOperationException("Wait for a character.");
        NativeHotbars.Ready((uint)Player.Job, pvp);
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] ||
            Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] ||
            Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
            throw new InvalidOperationException("Hotbar updates wait for combat and loading to finish.");
        if (Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "HotbarBridge" && p.IsLoaded))
            throw new InvalidOperationException("Disable or uninstall retired Hotbar Bridge before Accord changes hotbars.");
    }
    internal static bool Initialized(uint job) => Observed.GetValueOrDefault(
        NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))?.Contains(job) == true;
    internal static void ObservePvP(uint job)
    {
        Ready(true);
        if (Initialized(job)) return;
        var updated = Observed.ToDictionary(pair => pair.Key, pair => new HashSet<uint>(pair.Value));
        var key = NativeHotbars.CharacterKey(Svc.PlayerState.ContentId);
        if (!updated.TryGetValue(key, out var jobs)) updated[key] = jobs = new();
        jobs.Add(job);
        JsonStore.Write(PathName, updated);
        observed = updated;
    }
    internal static void Reset() => observed = null;
}
