namespace TaskOrchestrator.Shared.Contracts;

public record ColumnDto(
    int Id,
    int BoardId,
    string Title,
    string Color,
    int Position
);

public record ColumnWithCardsDto(
    int Id,
    int BoardId,
    string Title,
    string Color,
    int Position,
    IReadOnlyList<CardDto> Cards
);

public record CreateColumnRequest(int BoardId, string Title, string Color);

public record UpdateColumnRequest(string Title, string Color, int Position);
