using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Calendar;

/// <summary>Status of a user's linked calendar account (for the client UI).</summary>
public sealed record CalendarConnectionInfo(bool Connected, string? AccountEmail, DateTime? LastSyncUtc);

/// <summary>
/// Owns the stored Outlook connection: returns a fresh Graph access token by redeeming the
/// (encrypted) refresh token, and persists/clears connections. The refresh token is the durable
/// credential — it is encrypted at rest with Data Protection and never leaves the server.
/// </summary>
public interface ICalendarTokenProvider
{
    Task<CalendarConnectionInfo?> GetConnectionAsync(int userId);

    /// <summary>A fresh access token for the user, or null if they have no connection.</summary>
    Task<string?> GetAccessTokenAsync(int userId, CancellationToken ct = default);

    Task SaveConnectionAsync(int userId, OAuthTokens tokens);
    Task<bool> DeleteConnectionAsync(int userId);
}

public sealed class CalendarTokenProvider : ICalendarTokenProvider
{
    private const string Provider = "microsoft";

    private readonly TimeTrackerDbContext _db;
    private readonly OutlookOAuthClient _oauth;
    private readonly IDataProtector _protector;

    public CalendarTokenProvider(TimeTrackerDbContext db, OutlookOAuthClient oauth, IDataProtectionProvider dp)
    {
        _db = db;
        _oauth = oauth;
        _protector = dp.CreateProtector("TimeTracker.Calendar.RefreshToken");
    }

    public async Task<CalendarConnectionInfo?> GetConnectionAsync(int userId)
    {
        var c = await Load(userId);
        return c is null ? null : new CalendarConnectionInfo(true, c.AccountEmail, c.LastSyncUtc);
    }

    public async Task<string?> GetAccessTokenAsync(int userId, CancellationToken ct = default)
    {
        var connection = await Load(userId);
        if (connection is null)
        {
            return null;
        }

        var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
        var tokens = await _oauth.RefreshAsync(refreshToken, ct);

        // Entra rotates refresh tokens — persist the new one so the connection stays alive.
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            connection.RefreshTokenProtected = _protector.Protect(tokens.RefreshToken);
        }
        connection.LastSyncUtc = DateTime.UtcNow;
        connection.ModifiedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return tokens.AccessToken;
    }

    public async Task SaveConnectionAsync(int userId, OAuthTokens tokens)
    {
        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            throw new CalendarSourceException("Outlook did not return a refresh token. Ensure 'offline_access' is requested.");
        }

        var connection = await Load(userId);
        if (connection is null)
        {
            connection = new CalendarConnection
            {
                UserId = userId,
                Provider = Provider,
                CreatedUtc = DateTime.UtcNow,
            };
            _db.CalendarConnections.Add(connection);
        }
        else
        {
            connection.ModifiedUtc = DateTime.UtcNow;
        }

        connection.RefreshTokenProtected = _protector.Protect(tokens.RefreshToken);
        connection.AccountEmail = tokens.AccountEmail;
        connection.TenantId = tokens.TenantId;
        connection.Scopes = tokens.Scope ?? string.Empty;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteConnectionAsync(int userId)
    {
        var connection = await Load(userId);
        if (connection is null)
        {
            return false;
        }

        _db.CalendarConnections.Remove(connection);
        await _db.SaveChangesAsync();
        return true;
    }

    private Task<CalendarConnection?> Load(int userId) =>
        _db.CalendarConnections.FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == Provider);
}
