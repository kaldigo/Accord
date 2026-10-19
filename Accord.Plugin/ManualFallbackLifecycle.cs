using System;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Accord;

internal static unsafe class FallbackMode
{
    internal static bool Current => RaptureHotbarModule.Instance() != null && RaptureHotbarModule.Instance()->PvPHotbarsActive;
}

// Prepare before HandleLogout starts the lobby transition, not after the character unloads.
// This also handles an immediate logout in a sanctuary, where no countdown is available.
internal sealed unsafe class ManualFallbackLifecycle : IDisposable
{
    private delegate void LogoutDelegate(AgentLobby* agent, byte isExiting, byte framesUntilLogout);
    private readonly Hook<LogoutDelegate>? hook;
    private readonly Action prepare;
    internal string Error { get; } = "";
    internal ManualFallbackLifecycle(Action prepare)
    {
        this.prepare = prepare;
        try
        {
            nint address = AgentLobby.Addresses.HandleLogout.Value;
            if (address == 0) throw new InvalidOperationException("The logout address is unavailable.");
            hook = Svc.Hook.HookFromAddress<LogoutDelegate>(address, Detour);
            hook.Enable();
        }
        catch (Exception ex)
        {
            hook?.Dispose();
            Error = "Automatic logout fallback is unavailable. Use Prepare manual hotbars now before logging out.";
            Svc.Log.Error(ex, Error);
        }
    }
    private void Detour(AgentLobby* agent, byte isExiting, byte framesUntilLogout)
    {
        try
        {
            if (Player.Available && Svc.ClientState.IsLoggedIn)
            {
                if (!Svc.Framework.IsInFrameworkUpdateThread)
                    throw new InvalidOperationException("Logout was not on the game update thread. Prepare manual hotbars explicitly before exiting.");
                prepare();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Accord could not prepare manual hotbars before logout.");
            Svc.Chat.PrintError("Accord: manual hotbar fallback failed. " + ex.Message);
        }
        finally { hook!.Original(agent, isExiting, framesUntilLogout); }
    }
    public void Dispose() => hook?.Dispose();
}
