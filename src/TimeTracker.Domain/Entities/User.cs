namespace TimeTracker.Domain.Entities;

/// <summary>
/// Application-side user. Authentication is handled by the external identity
/// provider (Entra External ID); <see cref="ExternalId"/> stores the stable
/// <c>oid</c>/<c>sub</c> claim. No credentials are stored here. Table: <c>t_user</c>.
/// </summary>
public class User : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>Stable subject identifier from the identity provider (oid/sub claim).</summary>
    public string ExternalId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<UserOrganization> Organizations { get; set; } = new List<UserOrganization>();
}
