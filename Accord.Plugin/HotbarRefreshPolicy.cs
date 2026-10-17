using System.Collections.Generic;
using System.Linq;
using HotbarTools;

namespace Accord;

internal static class HotbarRefreshPolicy
{
    internal static bool NeedsApply(int editCount,
        Dictionary<uint, Dictionary<string, Position>> desired,
        Dictionary<uint, Dictionary<string, Position>> current) =>
        editCount != 0 || desired.Any(job => !current.TryGetValue(job.Key, out var routes) ||
            routes.Count != job.Value.Count || job.Value.Any(route =>
                !routes.TryGetValue(route.Key, out var position) || position != route.Value));
}
