using System;

namespace WrathCombo.Engine;

// Upstream migrations/notices use the inherited engine version, not the fork package version.
internal static class UpstreamVersion
{
    internal const string Text = "1.0.4.27";
    internal static readonly Version Current = new(Text);
}
