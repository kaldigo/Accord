using System;
using System.Collections.Generic;

namespace HotbarTools;

// Sprint may be dragged from General Actions or assigned directly as an action.
// Other general actions, macros and unknown types retain their original meaning.
public static class ModeActions
{
    public static bool IsSprint(SlotValue value) => value is { Type:1, Id:3 or 29057 } or { Type:10, Id:4 };
    public static SlotValue Canonical(SlotValue value) => IsSprint(value) ? new(1,3) : value;
    public static SlotValue ForMode(SlotValue value, bool pvp) => IsSprint(value)
        ? new(1, pvp ? 29057u : 3u) : value;
    public static List<SlotEdit> SprintEdits(uint job, bool pvp, Func<Position, SlotValue> read)
    {
        var edits = new List<SlotEdit>();
        for (int bar = 0; bar < 18; bar++)
            for (int slot = 0; slot < (bar < 10 ? 12 : 16); slot++)
            {
                var position = new Position(bar, slot);
                var value = read(position);
                var desired = ForMode(value, pvp);
                if (value != desired) edits.Add(new(job, position, value, desired));
            }
        return edits;
    }
    public static bool HasSharedSprint(Func<Position,bool> shared, Func<Position,SlotValue> read)
    {
        for(int bar=0;bar<18;bar++)
            for(int slot=0;slot<(bar<10?12:16);slot++)
            {
                var position=new Position(bar,slot);
                if(shared(position) && IsSprint(read(position)))return true;
            }
        return false;
    }
}
