using MudBlazor;

namespace TimeTracker.Web.Theme;

/// <summary>Brand theme for the TimeTracker app (light + dark palettes).</summary>
public static class AppTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2563EB",          // brand blue
            Secondary = "#0EA5E9",        // sky accent
            Tertiary = "#7C3AED",
            AppbarBackground = "#2563EB",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1F2933",
            DrawerIcon = "#52606D",
            Background = "#F6F8FB",
            Surface = "#FFFFFF",
            Info = "#0284C7",
            Success = "#16A34A",
            Warning = "#F59E0B",
            Error = "#DC2626",
            TextPrimary = "#1F2933",
            TextSecondary = "#52606D",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#5B8DEF",
            Secondary = "#38BDF8",
            Tertiary = "#A78BFA",
            AppbarBackground = "#1A1A27",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#1A1A27",
            DrawerText = "#E2E8F0",
            DrawerIcon = "#94A3B8",
            Background = "#15151F",
            Surface = "#1E1E2A",
            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#FBBF24",
            Error = "#F87171",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
