using MudBlazor;

namespace TimeTracker.Web.Theme;

/// <summary>One selectable icon: a stable <see cref="Key"/> stored on a field option,
/// a friendly <see cref="Display"/>, and the MudBlazor <see cref="Icon"/> path.</summary>
public record IconChoice(string Key, string Display, string Icon);

/// <summary>
/// Curated palette of icons an admin can assign to a select-field option (e.g. a Category).
/// Options store the stable key; the UI resolves the key to a MudBlazor icon for display.
/// </summary>
public static class EntryIconPalette
{
    public static readonly IReadOnlyList<IconChoice> Choices = new List<IconChoice>
    {
        new("meeting", "Meeting", Icons.Material.Filled.Groups),
        new("call", "Call", Icons.Material.Filled.Call),
        new("development", "Development", Icons.Material.Filled.Code),
        new("bug", "Bug fix", Icons.Material.Filled.BugReport),
        new("support", "Support", Icons.Material.Filled.SupportAgent),
        new("email", "Email", Icons.Material.Filled.Email),
        new("design", "Design", Icons.Material.Filled.DesignServices),
        new("research", "Research", Icons.Material.Filled.Science),
        new("documentation", "Documentation", Icons.Material.Filled.Description),
        new("planning", "Planning", Icons.Material.Filled.EventNote),
        new("review", "Review", Icons.Material.Filled.RateReview),
        new("training", "Training", Icons.Material.Filled.School),
        new("admin", "Admin", Icons.Material.Filled.Settings),
        new("travel", "Travel", Icons.Material.Filled.Flight),
        new("break", "Break", Icons.Material.Filled.FreeBreakfast),
        new("finance", "Finance", Icons.Material.Filled.AttachMoney),
    };

    private static readonly Dictionary<string, IconChoice> ByKey =
        Choices.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The MudBlazor icon path for a key, or null if unset/unknown.</summary>
    public static string? Resolve(string? key) =>
        !string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key, out var c) ? c.Icon : null;

    /// <summary>The friendly label for a key (falls back to the key itself).</summary>
    public static string Display(string? key) =>
        !string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key, out var c) ? c.Display : (key ?? "");
}
