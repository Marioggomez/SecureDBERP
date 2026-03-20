using System.Collections;
using System.Reflection;
using DevExpress.LookAndFeel;
using DevExpress.Skins;

namespace SecureERP.WinForms.Themes;

public static class DevExpressAppearanceCatalog
{
    private static readonly IReadOnlyList<SvgSkinFamily> _svgFamilies = BuildSvgFamilies();

    public static IReadOnlyList<string> GetInstalledSkinNames()
        => SkinManager.Default.Skins
            .Cast<SkinContainer>()
            .Select(skin => skin.SkinName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<SvgSkinFamily> GetSvgFamilies() => _svgFamilies;

    public static bool TryGetSvgPalette(string familyName, string paletteName, out SkinSvgPalette? palette)
    {
        palette = null;

        SvgSkinFamily? family = _svgFamilies.FirstOrDefault(item => string.Equals(item.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));
        if (family is null)
        {
            return false;
        }

        PropertyInfo? paletteSetProperty = family.Family.GetType().GetProperty("PaletteSet", BindingFlags.Public | BindingFlags.Instance);
        if (paletteSetProperty?.GetValue(family.Family) is not IDictionary paletteSet)
        {
            return false;
        }

        if (!paletteSet.Contains(paletteName))
        {
            return false;
        }

        palette = paletteSet[paletteName] as SkinSvgPalette;
        return palette is not null;
    }

    public static void ApplySkin(string skinName, string? paletteName = null)
    {
        if (!string.IsNullOrWhiteSpace(paletteName))
        {
            UserLookAndFeel.Default.SetSkinStyle(skinName, paletteName);
            return;
        }

        UserLookAndFeel.Default.SetSkinStyle(skinName);
    }

    public static void ApplyPalette(SkinSvgPalette palette) => UserLookAndFeel.Default.SetSkinStyle(palette);

    public static string? GetCurrentPaletteName() => UserLookAndFeel.Default.ActiveSvgPaletteName;

    private static IReadOnlyList<SvgSkinFamily> BuildSvgFamilies()
    {
        List<SvgSkinFamily> families = [];
        FieldInfo[] fields = typeof(SkinSvgPalette).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.GetValue(null) is not SkinSvgPalette family)
            {
                continue;
            }

            PropertyInfo? paletteSetProperty = family.GetType().GetProperty("PaletteSet", BindingFlags.Public | BindingFlags.Instance);
            if (paletteSetProperty?.GetValue(family) is not IDictionary paletteSet)
            {
                continue;
            }

            Dictionary<string, SkinSvgPalette> palettes = [];
            foreach (DictionaryEntry entry in paletteSet)
            {
                if (entry.Key is not string key || entry.Value is not SkinSvgPalette palette)
                {
                    continue;
                }

                palettes[key] = palette;
            }

            families.Add(new SvgSkinFamily(field.Name, family, palettes));
        }

        return families
            .OrderBy(item => item.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record SvgSkinFamily(string FamilyName, SkinSvgPalette Family, IReadOnlyDictionary<string, SkinSvgPalette> Palettes);
