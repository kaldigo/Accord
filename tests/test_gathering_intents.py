"""Compile production context selectors; native state reads are checked by the full build."""
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


class GatheringIntentTests(unittest.TestCase):
    def test_contexts_preserve_manual_choices(self):
        source = (ROOT / 'WrathCombo/Combos/PvE/DOL/GatheringIntents.cs').read_text()
        selectors = source[source.index('    internal static uint GatheringIntent'):source.index('    internal class DOL_ContextTools')]
        dol = (ROOT / 'WrathCombo/Combos/PvE/DOL/DOL.cs').read_text()
        constants = dol[dol.index('    internal const uint'):dol.index('    internal static class Buffs')]
        harness = r'''
using System;
using System.Collections.Generic;
enum FishingState { None, CastingOut, PullingPoleIn, Quitting, PoleReady, Bite, Hooking, ReleasingCatch, ConfirmingCollectable, AmbitiousLure, ModestLure, Unk11, LineInWater }
class DOL {
CONSTANTS
SELECTORS
}
class Program {
    static int checks;
    static void Equal(uint actual,uint expected,string label) { checks++; if(actual!=expected)throw new Exception($"{label}: {actual} != {expected}"); }
    static void Main() {
        // Independent action ledger: maps are intentionally not read from the implementation.
        var collect = new Dictionary<uint,uint> { [4073]=22182,[272]=22182,[4087]=22186,[273]=22186,[241]=22184,[224]=22188,[295]=22183,[294]=22187,[25589]=21205,[25590]=21206,[21203]=34871,[21204]=34872,[4589]=22185,[4590]=22189,[280]=240,[282]=815 };
        var travel = new Dictionary<uint,uint> { [4072]=227,[4086]=210,[4081]=238,[4095]=221,[235]=228,[218]=211,[237]=291,[220]=290,[239]=303,[222]=304 };
        var rod = new Dictionary<uint,uint> { [289]=296,[4102]=4179,[4106]=4103,[26806]=269,[37046]=27523,[4595]=37595,[4596]=37594 };
        var spear = new Dictionary<uint,uint> { [289]=7632,[288]=7903,[268]=7905,[26805]=7904,[37045]=7911,[4595]=7906,[26806]=7909,[4100]=7910,[2135]=26872,[4105]=26870,[4104]=26871 };
        // Exhaustive IDs include unrelated jobs and every manual control. GP, cooldown and
        // unlock changes cannot substitute a different spending tier or a prep buff at a bite.
        for(uint action=1;action<=40000;action++) {
            Equal(DOL.GatheringIntent(action,false,false),travel.GetValueOrDefault(action,action),"travel");
            Equal(DOL.GatheringIntent(action,true,false),action,"ordinary node/manual tiers");
            Equal(DOL.GatheringIntent(action,true,true),collect.GetValueOrDefault(action,action),"collectable/manual appraisal");
            foreach(bool free in new[]{false,true}) {
                Equal(DOL.FishingIntent(action,false,false,free),action==268&&free?297:action,"free/paid mooch");
                Equal(DOL.FishingIntent(action,false,true,free),rod.GetValueOrDefault(action,action),"line out");
                foreach(bool line in new[]{false,true})
                    Equal(DOL.FishingIntent(action,true,line,free),spear.GetValueOrDefault(action,action),"diving wins");
            }
        }
        foreach(var state in Enum.GetValues<FishingState>()) {
            bool expected=state is FishingState.CastingOut or FishingState.LineInWater or FishingState.Bite or FishingState.Hooking or FishingState.AmbitiousLure or FishingState.ModestLure;
            Equal(DOL.LineOut(state)?1u:0u,expected?1u:0u,"fishing state");
        }
        Equal(DOL.LineOut((FishingState)999)?1u:0u,0,"unknown state");
        Console.WriteLine($"{checks} gathering context checks passed");
    }
}
'''.replace('CONSTANTS', constants).replace('SELECTORS', selectors)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>')
            (path / 'Program.cs').write_text(harness)
            result = subprocess.run(['dotnet', 'run', '--project', str(path / 'Test.csproj'), '-p:UseSharedCompilation=false'], capture_output=True, text=True, timeout=120)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn('440014 gathering context checks passed', result.stdout)

    def test_native_guards_and_opt_in(self):
        source = (ROOT / 'WrathCombo/Combos/PvE/DOL/GatheringIntents.cs').read_text()
        self.assertIn('gathering && GenericHelpers.TryGetAddonByName', source)
        self.assertIn('addon->IsVisible', source)
        self.assertIn('fishing != null && LineOut(fishing->State)', source)
        self.assertIn('GetActionStatus(ActionType.Action, Mooch) == 0', source)
        base = (ROOT / 'WrathCombo/CustomCombo/CustomCombo.cs').read_text()
        self.assertLess(base.index('if (!IsEnabled(Preset))'), base.index('Invoke(actionID)'))
        self.assertNotIn('UseAction(', source)
