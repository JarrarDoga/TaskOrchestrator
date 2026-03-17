CREATE TABLE IF NOT EXISTS CardActivity (
    Id              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    CardId          INT          NOT NULL,
    BoardId         INT          NOT NULL,
    EventType       TINYINT      NOT NULL COMMENT 'Maps to ActivityEventType enum',
    UserId          VARCHAR(128)     NULL,
    UserDisplayName VARCHAR(200)     NULL,
    Description     VARCHAR(500) NOT NULL,
    OccurredAtUtc   DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),

    CONSTRAINT fk_activity_card FOREIGN KEY (CardId) REFERENCES Cards(Id) ON DELETE CASCADE,
    INDEX idx_activity_card    (CardId, OccurredAtUtc),
    INDEX idx_activity_board   (BoardId, OccurredAtUtc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
