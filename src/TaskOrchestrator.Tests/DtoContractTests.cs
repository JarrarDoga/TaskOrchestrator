using System.Text.Json;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Tests;

/// <summary>
/// Verifies that DTO contracts serialize correctly and the solution builds
/// with correct references to TaskOrchestrator.Shared and TaskOrchestrator.Api.
/// </summary>
public class DtoContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // ----- 1) DTO Serialization -----

    [Fact]
    public void CardDto_SerializesAndDeserializes_RoundTrip()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var metaJson = JsonSerializer.Deserialize<JsonElement>("{\"custom_field\": \"custom_value\"}");
        var original = new CardDto(
            Id: 42,
            BoardId: 1,
            ColumnId: 2,
            Title: "Implement auth",
            Description: "Add JWT bearer auth",
            Position: 3,
            Version: 1,
            Priority: TaskPriority.High,
            AssignedToUserId: "user-123",
            Attachments: Array.Empty<AttachmentDto>(),
            Metadata: new Dictionary<string, JsonElement> { { "custom_field", metaJson } },
            UpdatedAtUtc: now,
            UpdatedByUserId: "user-456"
        );

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CardDto>(json, JsonOptions)!;

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Title, deserialized.Title);
        Assert.Equal(original.Priority, deserialized.Priority);
        Assert.Equal(original.BoardId, deserialized.BoardId);
        Assert.Equal(original.AssignedToUserId, deserialized.AssignedToUserId);
    }

    [Fact]
    public void BoardDetailDto_SerializesColumnsAndCards()
    {
        // Arrange
        var columnCards = new List<CardDto>
        {
            new(1, 1, 1, "Card 1", null, 0, 1, TaskPriority.Low,
                null, Array.Empty<AttachmentDto>(),
                new Dictionary<string, JsonElement>(), DateTime.UtcNow, null),
            new(2, 1, 1, "Card 2", "Description here", 1, 1, TaskPriority.Medium,
                "user-1", Array.Empty<AttachmentDto>(),
                new Dictionary<string, JsonElement>(), DateTime.UtcNow, "user-1")
        };

        var column = new ColumnWithCardsDto(
            Id: 1, BoardId: 1, Title: "To Do", Color: "#3498db", Position: 0,
            Cards: columnCards
        );

        var detail = new BoardDetailDto(
            Id: 1, Name: "Sprint Board", Description: "Q1 sprint",
            TeamId: 10, TeamName: "Backend Team",
            CreatedAt: DateTime.UtcNow, Version: 5,
            Columns: new[] { column }
        );

        // Act
        var json = JsonSerializer.Serialize(detail, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<BoardDetailDto>(json, JsonOptions)!;

        // Assert
        Assert.NotNull(json);
        Assert.Contains("Sprint Board", json);
        Assert.Equal(detail.Id, roundTrip.Id);
        Assert.Equal(detail.Name, roundTrip.Name);
        Assert.Equal(detail.Version, roundTrip.Version);
        Assert.Single(roundTrip.Columns);
        Assert.Equal(2, roundTrip.Columns[0].Cards.Count);
        Assert.Equal("Card 1", roundTrip.Columns[0].Cards[0].Title);
        Assert.Equal("Card 2", roundTrip.Columns[0].Cards[1].Title);
    }

    // ----- 2) CardDto Validation -----

    [Fact]
    public void CreateCardRequest_HasRequiredFields()
    {
        // Arrange & Act
        var request = new CreateCardRequest(
            BoardId: 1,
            ColumnId: 2,
            Title: "New card",
            Description: "A detailed description",
            Priority: TaskPriority.Critical
        );

        // Assert
        Assert.Equal(1, request.BoardId);
        Assert.Equal(2, request.ColumnId);
        Assert.Equal("New card", request.Title);
        Assert.Equal("A detailed description", request.Description);
        Assert.Equal(TaskPriority.Critical, request.Priority);
    }

    [Fact]
    public void CardDto_NullableFieldsAcceptNull()
    {
        // Arrange & Act
        var card = new CardDto(
            Id: 1, BoardId: 1, ColumnId: 1,
            Title: "Minimal card",
            Description: null,
            Position: 0, Version: 1,
            Priority: TaskPriority.Low,
            AssignedToUserId: null,
            Attachments: Array.Empty<AttachmentDto>(),
            Metadata: new Dictionary<string, JsonElement>(),
            UpdatedAtUtc: DateTime.UtcNow,
            UpdatedByUserId: null
        );

        // Assert
        Assert.Null(card.Description);
        Assert.Null(card.AssignedToUserId);
        Assert.Null(card.UpdatedByUserId);
        Assert.Empty(card.Attachments);
        Assert.Empty(card.Metadata);
    }

    // ----- 3) BoardDto Validation -----

    [Fact]
    public void BoardDto_ContainsExpectedFields()
    {
        // Arrange & Act
        var board = new BoardDto(
            Id: 100,
            Name: "Marketing Board",
            Description: "Marketing sprint tracking",
            TeamId: 5,
            TeamName: "Marketing",
            CreatedAt: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Version: 3
        );

        // Assert
        Assert.Equal(100, board.Id);
        Assert.Equal("Marketing Board", board.Name);
        Assert.Equal("Marketing sprint tracking", board.Description);
        Assert.Equal(5, board.TeamId);
        Assert.Equal("Marketing", board.TeamName);
        Assert.Equal(3, board.Version);
        Assert.Equal(new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), board.CreatedAt);
    }

    [Fact]
    public void BoardDto_OptionalTeamFieldsAcceptNull()
    {
        // Arrange & Act
        var personalBoard = new BoardDto(
            Id: 1, Name: "My Board", Description: null,
            TeamId: null, TeamName: null,
            CreatedAt: DateTime.UtcNow, Version: 1
        );

        // Assert
        Assert.Null(personalBoard.Description);
        Assert.Null(personalBoard.TeamId);
        Assert.Null(personalBoard.TeamName);
    }

    [Fact]
    public void CreateBoardRequest_ValidatesName()
    {
        // Arrange
        var request = new CreateBoardRequest("New Board", "Description", 1);

        // Assert
        Assert.Equal("New Board", request.Name);
        Assert.Equal("Description", request.Description);
        Assert.Equal(1, request.TeamId);
    }

    [Fact]
    public void UpdateBoardRequest_RequiresVersion()
    {
        // Arrange
        var request = new UpdateBoardRequest("Updated Name", "Updated Desc", 2);

        // Assert
        Assert.Equal("Updated Name", request.Name);
        Assert.Equal(2, request.Version);
    }

    // ----- 4) ColumnDto Validation -----

    [Fact]
    public void ColumnDto_ContainsExpectedFields()
    {
        // Arrange & Act
        var column = new ColumnDto(
            Id: 10,
            BoardId: 1,
            Title: "In Progress",
            Color: "#f39c12",
            Position: 1
        );

        // Assert
        Assert.Equal(10, column.Id);
        Assert.Equal(1, column.BoardId);
        Assert.Equal("In Progress", column.Title);
        Assert.Equal("#f39c12", column.Color);
        Assert.Equal(1, column.Position);
    }

    [Fact]
    public void ColumnWithCardsDto_EmptyCardsList()
    {
        // Arrange & Act
        var emptyColumn = new ColumnWithCardsDto(
            Id: 1, BoardId: 1, Title: "Done", Color: "#2ecc71", Position: 2,
            Cards: Array.Empty<CardDto>()
        );

        // Assert
        Assert.Empty(emptyColumn.Cards);
        Assert.Equal("Done", emptyColumn.Title);
    }

    [Fact]
    public void CreateColumnRequest_HasRequiredFields()
    {
        // Arrange
        var request = new CreateColumnRequest(
            BoardId: 1, Title: "Review", Color: "#9b59b6"
        );

        // Assert
        Assert.Equal(1, request.BoardId);
        Assert.Equal("Review", request.Title);
        Assert.Equal("#9b59b6", request.Color);
    }

    // ----- 5) TaskPriority Enum Values -----

    [Theory]
    [InlineData(TaskPriority.Low, 0, "Low")]
    [InlineData(TaskPriority.Medium, 1, "Medium")]
    [InlineData(TaskPriority.High, 2, "High")]
    [InlineData(TaskPriority.Critical, 3, "Critical")]
    public void TaskPriority_EnumValuesAreCorrect(
        TaskPriority value,
        int expectedOrdinal,
        string expectedName)
    {
        // Assert
        Assert.Equal(expectedOrdinal, (int)value);
        Assert.Equal(expectedName, value.ToString());
    }

    [Fact]
    public void TaskPriority_EnumCount_IsFour()
    {
        // Assert
        Assert.Equal(4, Enum.GetValues<TaskPriority>().Length);
    }
}
