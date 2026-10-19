"""Exercise the actual Auto Mode DPS dispatch with simulated target selection.

The native game cannot run in CI. Compile the production dispatch method unchanged
with minimal game/rotation stubs and observe whether it reaches ST/AoE execution.
"""
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


class DancerPrecastTests(unittest.TestCase):
    def test_selected_enemy_required_before_out_of_combat_dispatch(self):
        source = (ROOT / 'WrathCombo/AutoRotation/AutoRotationController.cs').read_text(encoding='utf-8-sig')
        start = source.index('    private static bool AutomateDPS(')
        body = source.index('{', start)
        depth = 1
        end = body + 1
        while depth:
            depth += (source[end] == '{') - (source[end] == '}')
            end += 1
        method = source[start:end].replace('private static', 'public static', 1)
        harness = r'''
using System;
enum Job { DNC, Other }
enum Preset { Rotation }
enum Mode { Manual, Nearest }
static class Player { public static Job Job; }
class Target {
    public bool IsDead { get; set; }
    public bool IsTargetable { get; set; } = true;
    public bool Hostile;
    public bool IsHostile() => Hostile;
}
static class SimpleTarget { public static Target? HardTarget; }
class AutoAction { public bool IsAoE; }
static class PresetStorage {
    public class PresetData { public AutoAction AutoAction = new(); }
}
class Config { public Mode DPSRotationMode; }
static class AutoRotationHelper {
    public static int ST, AoE;
    public static Target? OverrideTarget = new() { Hostile = true };
    public static bool ExecuteST(Mode mode, Preset preset, PresetStorage.PresetData data, uint action) { ST++; return true; }
    public static bool ExecuteAoE(Mode mode, Preset preset, PresetStorage.PresetData data, uint action) { AoE++; return true; }
}
class Dispatch {
    public static Config cfg = new();
    public static bool Combat;
    static bool InCombat() => Combat;
METHOD
}
class Program {
    static void Main() {
        var friendly = new Target(); // Party member or friendly battle NPC.
        var enemy = new Target { Hostile = true };
        Target?[] targets = [null, friendly, enemy,
            new() { Hostile = true, IsDead = true },
            new() { Hostile = true, IsTargetable = false }];
        int checks = 0;
        foreach (var job in new[] { Job.DNC, Job.Other })
        foreach (var combat in new[] { false, true })
        foreach (var mode in Enum.GetValues<Mode>())
        foreach (var aoe in new[] { false, true })
        foreach (var target in targets) {
            Player.Job = job; Dispatch.Combat = combat; Dispatch.cfg.DPSRotationMode = mode;
            SimpleTarget.HardTarget = target;
            AutoRotationHelper.ST = AutoRotationHelper.AoE = 0;
            var data = new PresetStorage.PresetData(); data.AutoAction.IsAoE = aoe;
            bool result = Dispatch.AutomateDPS(Preset.Rotation, data, 123);
            bool expected = job != Job.DNC || combat || ReferenceEquals(target, enemy);
            if (result != expected || AutoRotationHelper.ST != (expected && !aoe ? 1 : 0) ||
                AutoRotationHelper.AoE != (expected && aoe ? 1 : 0))
                throw new Exception($"Unexpected dispatch: {job}/{combat}/{mode}/{aoe}/{Array.IndexOf(targets,target)}");
            checks++;
        }
        Console.WriteLine($"{checks} target/rotation scenarios passed; an automatic enemy override cannot authorize precasting.");
    }
}
'''.replace('METHOD', method)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path / 'Dispatch.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup></Project>')
            (path / 'Program.cs').write_text(harness)
            result = subprocess.run(['dotnet', 'run', '--project', str(path / 'Dispatch.csproj'), '-p:UseSharedCompilation=false'], capture_output=True, text=True, timeout=120)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn('80 target/rotation scenarios passed', result.stdout)
