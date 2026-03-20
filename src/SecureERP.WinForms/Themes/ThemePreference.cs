using SecureERP.WinForms.Common;

namespace SecureERP.WinForms.Themes;

public sealed record ThemePreference(
    string SkinName,
    string? PaletteName = null,
    string? AccentColorHex = null,
    bool RoundedWindowCorners = true,
    bool CompactUIMode = false)
{
    public static ThemePreference Default { get; } = new(UiConstants.DefaultSkin);
}
