// Simulated assigned slots for exercising the production planners and transactions.
using System.Runtime.InteropServices;
using HotbarTools;
namespace ECommons.GameHelpers
{
    internal static class Player { internal static bool Available = true; internal static uint Job = 19; }
}
namespace Lumina.Excel.Sheets { public struct Action {} public struct CraftAction {} public struct ActionComboRoute {} }
namespace ECommons.DalamudServices
{
    internal static class Svc
    {
        internal static readonly FakePlugin PluginInterface = new();
        internal static readonly FakePlayer PlayerState = new();
        internal static readonly FakeClient ClientState = new();
        internal static readonly FakeConditions Condition = new();
        internal static readonly FakeData Data = new();
    }
    internal sealed class FakePlugin
    {
        internal string Root = "";
        internal FileInfo ConfigFile => new(Path.Combine(Root, "Accord.json"));
        internal string GetPluginConfigDirectory() => Path.Combine(Root, "Accord");
        internal List<FakeInstalled> InstalledPlugins = new();
    }
    internal sealed class FakeInstalled { public string InternalName = ""; public bool IsLoaded; }
    internal sealed class FakePlayer { internal ulong ContentId = 123; }
    internal sealed class FakeClient { internal bool IsLoggedIn = true; }
    internal sealed class FakeConditions
    {
        internal bool Blocked;
        internal bool this[Dalamud.Game.ClientState.Conditions.ConditionFlag flag] => Blocked;
    }
    internal sealed class FakeData
    {
        internal FakeSheet<T> GetExcelSheet<T>(Dalamud.Game.ClientLanguage language) => new();
    }
    internal sealed class FakeSheet<T> { internal FakeRow GetRow(uint id) => new(); }
    internal sealed class FakeRow
    {
        internal FakeName Name => new();
        internal bool IsPvP => true;
        internal bool IsPlayerAction => true;
        internal FakeRef[] Action => [new()];
    }
    internal sealed class FakeName { internal bool IsEmpty => false; }
    internal sealed class FakeRef { internal uint RowId => 1; internal FakeRow Value => new(); }
}
namespace HotbarTools
{
    internal sealed class ActionCatalog
    {
        internal ActionCatalog(object actions, object crafting) { }
        internal SlotValue ResolveSlot(string job, string action, uint? expectedId = null) => action == "Empty" ? default :
            action == "Sprint" ? new(1,3) : new(1,100 + (uint)action.Aggregate(0,(n,c)=>(n*31+c)&0xffff));
    }
    internal struct FakeHotbarModule
    {
        internal bool IsHotbarShared(uint bar) => NativeHotbars.Shared.Contains((int)bar);
    }
    internal static unsafe class NativeHotbars
    {
        private static readonly FakeHotbarModule* Module = (FakeHotbarModule*)NativeMemory.AllocZeroed((nuint)sizeof(FakeHotbarModule));
        internal static readonly HashSet<int> Shared = new();
        internal static readonly Dictionary<(bool Mode, uint Job, Position Position), SlotValue> Slots = new();
        internal static bool Mode;
        internal static int Writes;
        internal static int FailAt;
        internal static FakeHotbarModule* Ready(uint job, bool pvp = false)
        {
            if (job != ECommons.GameHelpers.Player.Job || Mode != pvp) throw new InvalidOperationException("Not ready");
            return Module;
        }
        internal static SlotValue Read(uint job, Position p, bool live = true, bool pvp = false)
        {
            if (!p.Valid) throw new InvalidDataException("Invalid position");
            if (live && pvp != Mode) throw new InvalidOperationException("Wrong mode");
            return Slots.GetValueOrDefault((pvp, Shared.Contains(p.Bar) ? 0 : job, p));
        }
        internal static void Put(uint job, Position p, SlotValue value, bool pvp = false) => Slots[(pvp, Shared.Contains(p.Bar) ? 0 : job, p)] = value;
        internal static void Write(uint current, uint job, Position p, SlotValue value, bool pvp = false, bool? activePvP = null)
        {
            Ready(current, activePvP ?? pvp);
            Put(job,p,value,pvp);
            if (++Writes == FailAt) throw new IOException("Simulated read-back failure after write");
        }
        internal static void Apply(uint current, IReadOnlyList<SlotEdit> edits, string directory, bool pvp = false)
        {
            JsonStore.Write(Path.Combine(directory, "test-backup.json"), edits);
            Accord.HotbarTransaction.Apply(edits, (e,undo) => Write(current,e.Job,e.Position,undo?e.Before:e.After,pvp),null,null);
        }
        internal static string CharacterKey(ulong id) => id.ToString();
        internal static int Flushes;
        internal static void Flush(uint current, bool pvp) { Ready(current,pvp); Flushes++; }
    }
}
namespace Accord
{
    internal static class FallbackMode { internal static bool Current => HotbarTools.NativeHotbars.Mode; }
}
