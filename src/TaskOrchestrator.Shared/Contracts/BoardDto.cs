namespace TaskOrchestrator.Shared.Contracts;

public record BoardDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int Version
);

public record CreateBoardRequest(
    string Name,
    string? Description
);
