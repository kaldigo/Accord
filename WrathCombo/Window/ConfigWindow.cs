using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using WrathCombo.Core;
using WrathCombo.Data.Conflicts;
using WrathCombo.Resources.Localization.UI.MainWindow;
using WrathCombo.Services;
using WrathCombo.Window.Functions;
using WrathCombo.Window.Tabs;
using static WrathCombo.Core.PresetStorage;
using static WrathCombo.CustomComboNS.Functions.Jobs;
using Debug = WrathCombo.Window.Tabs.Debug;
using PunishGui = PunishLib.ImGuiMethods;
namespace WrathCombo.Window;

/// <summary> Plugin configuration window. </summary>
internal class ConfigWindow : Dalamud.Interface.Windowing.Window
{
    /// <summary>
    /// Dictionary of top level presets grouped by job, ordered by role and then job order, with their preset data pre-cached for quick access.
    /// </summary>
    internal static readonly Dictionary<Job, List<PresetData>> groupedPresets = GetGroupedPresets();

    /// <summary>
    ///  Dictionary of a preset and an array of it's children, with their preset data pre-cached for quick access.
    /// </summary>
    internal static readonly Dictionary<Preset, (Preset Preset, PresetData Attr)[]> presetChildren = GetPresetChildren();

    #region Search Variables
    internal static string Search = string.Empty;
    internal static string UsableSearch => Search.Trim().ToLowerInvariant();
    internal static bool SearchDescription = true;

    internal static bool IsSearching => !UsableSearch.IsNullOrWhitespace() &&
                                        UsableSearch.Length > 2;
    #endregion

    private static int GetRoleOrder(JobRole role) => role switch
    {
        JobRole.Tank => 0,
        JobRole.Healer => 1,
        JobRole.MeleeDPS => 2,
        JobRole.RangedDPS => 3,
        JobRole.MagicalDPS => 4,
        JobRole.Limited => 5,
        _ => 99
    };

    internal static Dictionary<Job, List<PresetData>> GetGroupedPresets()
    {
        return AllPresets
            .Where(kvp => (int)kvp.Key > 100)
            .Where(kvp => kvp.Value.Parent == null)
            .Where(kvp => kvp.Value.JobInfo != null)
            .OrderBy(kvp => kvp.Value.JobInfo.Job is Job.ADV ? 5 : GetRoleOrder(kvp.Value.JobInfo.Role))
            .ThenByDescending(kvp => kvp.Value.JobInfo.Job is Job.ADV)
            .ThenByDescending(kvp => kvp.Value.JobInfo.Job is Job.MIN)
            .ThenBy(kvp => kvp.Value.JobInfo.Job)
            .ThenBy(kvp => kvp.Value.JobInfo.Order)
            .GroupBy(kvp => kvp.Value.JobInfo.Job)
            .ToDictionary(
                g => g.Key,
                g => g.Select(kvp => kvp.Value).ToList()
            );
    }

    internal static Dictionary<Preset, (Preset Preset, PresetData Info)[]> GetPresetChildren()
    {
        // Initialize dictionary with all presets as keys
        var childCombos = AllPresets.Keys
            .ToDictionary(p => p, _ => new List<Preset>());

        // Build parent → children map using cached Parent
        foreach (var (preset, attrs) in AllPresets)
        {
            if (attrs.Parent is { } parent)
            {
                childCombos[parent].Add(preset);
            }
        }

        // Project to final structure using cached CustomComboInfo
        return childCombos.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Select(child =>
                {
                    var info = PresetStorage.AllPresets[child]!;
                    return (Preset: child, Info: info);
                })
                .OrderBy(tpl => tpl.Info.JobInfo.Order)
                .ToArray()
        );
    }

    public OpenWindow OpenWindow
    {
        get;
        set
        {
            ClearAnySearches();
            field = value;
        }
    } = OpenWindow.PvE;

    /// <summary> Initializes a new instance of the <see cref="ConfigWindow"/> class. </summary>
    public ConfigWindow() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###WrathFork")
    {
        RespectCloseHotkey = true;

        SetMinSize();

        Svc.PluginInterface.UiBuilder.DefaultFontHandle.ImFontChanged += SetMinSize;
    }

    private void SetMinSize(IFontHandle? fontHandle = null, ILockedImFont? lockedFont = null) =>
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 100),
        };

    public override void Draw()
    {
        if (ImGui.Button("PvE rotations")) OpenWindow = OpenWindow.PvE;
        ImGui.SameLine();
        if (ImGui.Button("Engine options")) OpenWindow = OpenWindow.Debug;
        ImGui.SameLine();
        if (ImGui.Button("Settings")) OpenWindow = OpenWindow.Settings;
        ImGui.Separator();
        if (OpenWindow == OpenWindow.Debug) Engine.OptionsUi.Draw();
        else if (OpenWindow == OpenWindow.Settings) Settings.Draw();
        else PvEFeatures.Draw();
    }

    public static void ClearAnySearches()
    {
        Search = string.Empty;
        SearchDescription = true;
    }
    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.DefaultFontHandle.ImFontChanged -= SetMinSize;
    }
}

public enum OpenWindow
{
    None = 0,
    PvE = 1,
    PvP = 2,
    Settings = 3,
    AutoRotation = 4,
    About = 5,
    Debug = 6,
    CustomActions = 7,
}
