namespace TaskOrchestrator.Shared.Contracts;

public record BoardDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int Version
);

// Full board load — one round trip on page open
public record BoardDetailDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int Version,
    IReadOnlyList<ColumnWithCardsDto> Columns
);

public record CreateBoardRequest(string Name, string? Description);

public record UpdateBoardRequest(string Name, string? Description, int Version);
