namespace TimeTracker.Domain.Entities;

/// <summary>
/// A user's linked external calendar account (Microsoft 365 via Graph). Holds the encrypted
/// refresh token used to mint short-lived access tokens server-side. One per user per provider.
/// Table: <c>t_calendar_connection</c>.
/// </summary>
public class CalendarConnection : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Provider key, e.g. "microsoft".</summary>
    public string Provider { get; set; } = "microsoft";

    /// <summary>The connected account's email/UPN, for display.</summary>
    public string? AccountEmail { get; set; }

    /// <summary>The account's home tenant id (from the token), informational.</summary>
    public string? TenantId { get; set; }

    /// <summary>Refresh token, encrypted at rest (ASP.NET Data Protection). Never returned to clients.</summary>
    public string RefreshTokenProtected { get; set; } = string.Empty;

    /// <summary>Scopes granted at connect time.</summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>Last time the calendar was successfully read.</summary>
    public DateTime? LastSyncUtc { get; set; }

    public User User { get; set; } = null!;
}
