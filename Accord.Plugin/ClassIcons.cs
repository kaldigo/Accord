using System;
using Dalamud.Utility;
using System.Collections.Generic;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Data.Files;

namespace Accord;

internal static class ClassIcons
{
    private static readonly Dictionary<uint, IDalamudTextureWrap?> GreyIcons = new();

    internal static IDalamudTextureWrap? Get(uint job, bool selected)
    {
        var lookup = new GameIconLookup((uint)ExcelJobHelper.GetIcon((Job)job), hiRes: true);
        if (selected) return Svc.Texture.GetFromGameIcon(lookup).GetWrapOrDefault();
        if (GreyIcons.TryGetValue(job, out var cached)) return cached;
        try
        {
            var texture = Svc.Data.GameData.GetFile<TexFile>(Svc.Texture.GetIconPath(lookup));
            if (texture is null) return GreyIcons[job] = null;
            var pixels = texture.GetRgbaImageData();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte grey = (byte)((pixels[i] * 54 + pixels[i + 1] * 183 + pixels[i + 2] * 19) / 256);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = grey;
            }
            return GreyIcons[job] = Svc.Texture.CreateFromRaw(
                RawImageSpecification.Rgba32(texture.Header.Width, texture.Header.Height), pixels);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Unable to load Accord class icon {Job}", job);
            return GreyIcons[job] = null;
        }
    }

    internal static void Dispose()
    {
        foreach (var texture in GreyIcons.Values) texture?.Dispose();
        GreyIcons.Clear();
    }
}
