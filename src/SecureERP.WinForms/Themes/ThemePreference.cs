using SecureERP.WinForms.Common;

namespace SecureERP.WinForms.Themes;

public sealed record ThemePreference(string SkinName)
{
    public static ThemePreference Default { get; } = new(UiConstants.DefaultSkin);
}
