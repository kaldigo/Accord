using System;
using System.Collections.Generic;
using System.Linq;
using HotbarTools;

namespace Accord;

internal static class HotbarTransaction
{
    internal static void Apply(IReadOnlyList<SlotEdit> edits, Action<SlotEdit, bool> write,
        Action? applyRoutes, Action? restoreRoutes)
    {
        var attempted = new List<SlotEdit>();
        try
        {
            foreach (var edit in edits)
            {
                // Include writes that fail their read-back after changing the slot.
                attempted.Add(edit);
                write(edit, false);
            }
            applyRoutes?.Invoke();
        }
        catch (Exception original)
        {
            var failures = new List<Exception> { original };
            foreach (var edit in attempted.AsEnumerable().Reverse())
                try { write(edit, true); }
                catch (Exception ex) { failures.Add(ex); }
            try { restoreRoutes?.Invoke(); }
            catch (Exception ex) { failures.Add(ex); }
            throw new AggregateException("Hotbar transaction failed; rollback was attempted for every touched slot and route.", failures);
        }
    }
}
