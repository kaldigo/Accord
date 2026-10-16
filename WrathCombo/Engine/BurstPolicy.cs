using System;

namespace WrathCombo.Engine;

/// <summary>Only multi-action commitments need a temporary preset handoff.
/// Single-initiator jobs already continue from their native status/gauge.</summary>
internal static class BurstPolicy
{
    internal static int Duration(uint job) => job switch
    {
        20 or 22 or 23 or 30 or 32 or 35 or 37 => 20_000,
        31 => 30_000, // Barrel's Hypercharged allowance; Wildfire still requires Hypercharge.
        _ => 0,
    };

    // Pinned engine preset IDs, intentionally excluding rotation parents and
    // unrelated class-mechanic flags from the imported AutoBurst deltas.
    internal static bool Allows(uint job, int preset) => job switch
    {
        20 => preset is 9008 or 9009 or 9011 or 9029 or 9030 or 9032,
        22 => preset is 6102 or 6103 or 6104 or 6202 or 6203 or 6204,
        23 => preset is 3017 or 3032,
        30 => preset is 10006 or 10007 or 10022 or 10023,
        31 => preset is 8108 or 8110 or 8307,
        32 => preset is 5013 or 5015 or 5016 or 5051 or 5054 or 5055,
        35 => preset is 13010 or 13011 or 13207 or 13208,
        37 => preset is 7008 or 7011 or 7201 or 7204,
        _ => false,
    };
}
