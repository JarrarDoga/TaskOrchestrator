using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence;

public sealed class TeamRepository(IDbConnectionFactory db) : ITeamRepository
{
    public async Task<IEnumerable<TeamDto>> GetAllAsync(string userId)
    {
        using var conn = db.CreateConnection();

        // Return teams the user belongs to, plus all public teams
        var teams = (await conn.QueryAsync<TeamRow>(
            """
            SELECT DISTINCT t.Id, t.Name, t.Description, t.Slug, t.Icon, t.IsPublic,
                   t.CreatedAt, t.CreatedByUserId,
                   COUNT(b.Id) AS BoardCount
            FROM Teams t
            LEFT JOIN TeamMembers tm ON tm.TeamId = t.Id AND tm.UserId = @UserId
            LEFT JOIN Boards b ON b.TeamId = t.Id
            WHERE t.IsPublic = 1 OR tm.UserId IS NOT NULL
            GROUP BY t.Id, t.Name, t.Description, t.Slug, t.Icon, t.IsPublic, t.CreatedAt, t.CreatedByUserId
            ORDER BY t.CreatedAt DESC
            """,
            new { UserId = userId })).ToList();

        if (teams.Count == 0) return [];

        var teamIds = teams.Select(t => t.Id).ToList();
        var members = (await conn.QueryAsync<TeamMemberRow>(
            """
            SELECT tm.TeamId, tm.UserId, u.DisplayName, u.AvatarUrl, tm.Role, tm.JoinedAt
            FROM TeamMembers tm
            INNER JOIN Users u ON u.Id = tm.UserId
            WHERE tm.TeamId IN @TeamIds
            ORDER BY tm.JoinedAt
            """,
            new { TeamIds = teamIds }))
            .GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return teams.Select(t => ToDto(t, members.GetValueOrDefault(t.Id, [])));
    }

    public async Task<TeamDto?> GetByIdAsync(int teamId)
    {
        using var conn = db.CreateConnection();

        var team = await conn.QuerySingleOrDefaultAsync<TeamRow>(
            """
            SELECT t.Id, t.Name, t.Description, t.Slug, t.Icon, t.IsPublic,
                   t.CreatedAt, t.CreatedByUserId,
                   COUNT(b.Id) AS BoardCount
            FROM Teams t
            LEFT JOIN Boards b ON b.TeamId = t.Id
            WHERE t.Id = @Id
            GROUP BY t.Id, t.Name, t.Description, t.Slug, t.Icon, t.IsPublic, t.CreatedAt, t.CreatedByUserId
            """,
            new { Id = teamId });

        if (team is null) return null;

        var members = (await conn.QueryAsync<TeamMemberRow>(
            """
            SELECT tm.TeamId, tm.UserId, u.DisplayName, u.AvatarUrl, tm.Role, tm.JoinedAt
            FROM TeamMembers tm
            INNER JOIN Users u ON u.Id = tm.UserId
            WHERE tm.TeamId = @TeamId
            ORDER BY tm.JoinedAt
            """,
            new { TeamId = teamId })).ToList();

        return ToDto(team, members);
    }

    public async Task<IEnumerable<TeamBoardDto>> GetBoardsAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<TeamBoardDto>(
            """
            SELECT b.Id, b.Name, b.Description, b.CreatedAt, b.Version
            FROM Boards b
            INNER JOIN BoardMembers bm ON bm.BoardId = b.Id AND bm.UserId = @UserId
            WHERE b.TeamId = @TeamId
            ORDER BY b.CreatedAt DESC
            """,
            new { TeamId = teamId, UserId = userId });
    }

    public async Task<TeamDto> CreateAsync(CreateTeamRequest request, string ownerUserId)
    {
        using var conn = db.CreateConnection();

        var slug = GenerateSlug(request.Name);
        var icon = string.IsNullOrWhiteSpace(request.Icon) ? "group" : request.Icon;

        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Teams (Name, Description, Slug, Icon, IsPublic, CreatedByUserId)
            VALUES (@Name, @Description, @Slug, @Icon, @IsPublic, @CreatedByUserId);
            SELECT LAST_INSERT_ID();
            """,
            new
            {
                request.Name,
                Description = request.Description,
                Slug = slug,
                Icon = icon,
                IsPublic = request.IsPublic ? 1 : 0,
                CreatedByUserId = ownerUserId,
            });

        // Owner is always a member
        await conn.ExecuteAsync(
            "INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (@TeamId, @UserId, 'Owner')",
            new { TeamId = id, UserId = ownerUserId });

        // Add any additional members
        if (request.MemberUserIds is { Count: > 0 })
        {
            foreach (var memberId in request.MemberUserIds.Where(m => m != ownerUserId))
            {
                await conn.ExecuteAsync(
                    "INSERT IGNORE INTO TeamMembers (TeamId, UserId, Role) VALUES (@TeamId, @UserId, 'Member')",
                    new { TeamId = id, UserId = memberId });
            }
        }

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int teamId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Teams WHERE Id = @Id", new { Id = teamId });
        return rows > 0;
    }

    public async Task<bool> IsOwnerAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        var role = await conn.ExecuteScalarAsync<string?>(
            "SELECT Role FROM TeamMembers WHERE TeamId = @TeamId AND UserId = @UserId",
            new { TeamId = teamId, UserId = userId });
        return role == "Owner";
    }

    public async Task<bool> IsMemberAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM TeamMembers WHERE TeamId = @TeamId AND UserId = @UserId",
            new { TeamId = teamId, UserId = userId });
        return count > 0;
    }

    public async Task AddMemberAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO TeamMembers (TeamId, UserId, Role) VALUES (@TeamId, @UserId, 'Member')",
            new { TeamId = teamId, UserId = userId });
    }

    public async Task RemoveMemberAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM TeamMembers WHERE TeamId = @TeamId AND UserId = @UserId AND Role != 'Owner'",
            new { TeamId = teamId, UserId = userId });
    }

    // ---- helpers ----

    static TeamDto ToDto(TeamRow t, List<TeamMemberRow> members) =>
        new(
            t.Id,
            t.Name,
            t.Description,
            t.Slug,
            t.Icon,
            t.IsPublic,
            members.Count,
            (int)Math.Min(int.MaxValue, t.BoardCount),
            t.CreatedAt,
            t.CreatedByUserId,
            members.Select(m => new TeamMemberDto(m.UserId, m.DisplayName, m.AvatarUrl, m.Role, m.JoinedAt)).ToList()
        );

    static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

    // Private projection types
    sealed record TeamRow(
        int Id, string Name, string? Description, string Slug, string Icon,
        bool IsPublic, DateTime CreatedAt, string CreatedByUserId, long BoardCount);

    sealed record TeamMemberRow(
        int TeamId, string UserId, string DisplayName, string? AvatarUrl, string Role, DateTime JoinedAt);
}
