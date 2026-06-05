namespace TimeTracker.Domain.Entities;

/// <summary>
/// A user explicitly granted access to a restricted <see cref="Project"/>. Ignored when the
/// project's <see cref="Project.IsRestricted"/> is false. Table: <c>t_project_member</c>.
/// </summary>
public class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
