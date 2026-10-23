"""Reproduce native name/tooltip callbacks before the global setup is published."""
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


class CustomActionStartupTests(unittest.TestCase):
    def test_callbacks_during_construction_and_reload(self):
        source = (ROOT / 'WrathCombo/Native/CustomActionManager.cs').read_text()
        start = source.index('    private byte* FormatNameDetour(')
        end = source.index('    public IEnumerable<CustomAction> Actions', start)
        methods = source[start:end].replace('private byte*', 'public byte*').replace('private void*', 'public void*')
        harness = r'''
using System;
using System.Collections.Generic;
using System.Linq;
class CustomAction { public uint Id; public nint NamePtr,TransientRowPtr; }
class Plugin { public Setup? CustomActions; }
class Setup { public Manager? Manager; }
static class Extensions {
    public static bool TryGetFirst<T>(this IEnumerable<T> source,Func<T,bool> test,out T value) {
        value=source.FirstOrDefault(test)!;return value is not null;
    }
}
unsafe class Manager {
    public static Plugin? P;
    public readonly Dictionary<uint,CustomAction> _actions=new();
    public IEnumerable<CustomAction> Actions=>_actions.Values;
    public int Names,Tooltips;
    public unsafe delegate byte* NameDelegate(int nameType,uint rowId,uint detailType,uint parameter);
    public unsafe delegate void* TooltipDelegate(uint rowId);
    class Hook<T>(T original) { public T Original=original; }
    readonly Hook<NameDelegate> _updateNameHook;
    readonly Hook<TooltipDelegate> _updateTooltipHook;
    public Manager() {
        _updateNameHook=new((type,id,detail,param)=>{Names++;if(type!=7||detail!=8||param!=9)throw new Exception("Lost name arguments");return (byte*)101;});
        _updateTooltipHook=new(id=>{Tooltips++;return (void*)102;});
        // The real constructor enables both hooks before its caller assigns Manager
        // or P.CustomActions. Native calls may arrive in that interval.
        Verify(42,101,102);
    }
METHODS
    public void Verify(uint id,nint name,nint tooltip) {
        if((nint)FormatNameDetour(7,id,8,9)!=name || (nint)UpdateTooltipDetour(id)!=tooltip)
            throw new Exception("Incorrect callback result");
    }
}
unsafe class Program {
    static void Main() {
        // Null plugin, null setup, null manager, live old manager during replacement.
        foreach(int phase in Enumerable.Range(0,4)) {
            Manager.P=phase==0?null:new Plugin();
            if(phase>=2)Manager.P!.CustomActions=new Setup();
            Manager? old=null;
            if(phase==3){old=new Manager();old._actions[42]=new(){Id=42,NamePtr=201,TransientRowPtr=202};Manager.P!.CustomActions!.Manager=old;}
            var current=new Manager();
            current._actions[42]=new(){Id=42,NamePtr=301,TransientRowPtr=302};
            current.Verify(42,301,302); // Must not read old/global manager's pointers.
            current.Verify(77,101,102); // Real actions always reach original callbacks.
            if(current.Names!=2||current.Tooltips!=2)throw new Exception("Wrong fallback count");
            Manager.P=null;
            current.Verify(42,301,302);
            current._actions.Clear();current.Verify(42,101,102);
            old?.Verify(42,201,202);
        }
        Console.WriteLine("Startup/reload name and tooltip callbacks passed");
    }
}
'''.replace('METHODS', methods)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks><Nullable>enable</Nullable></PropertyGroup></Project>')
            (path / 'Program.cs').write_text(harness)
            result = subprocess.run(['dotnet', 'run', '--project', str(path / 'Test.csproj'), '-p:UseSharedCompilation=false'], capture_output=True, text=True, timeout=120)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn('Startup/reload name and tooltip callbacks passed', result.stdout)
