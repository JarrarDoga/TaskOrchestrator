-- Relational columns: anything you filter/sort/join on.
-- Metadata (JSON): labels, custom fields, UI hints — avoids schema churn.
--
-- MariaDB JSON type is an alias for LONGTEXT with JSON_VALID() validation.
-- Dapper maps it as string; the repository deserialises it.

CREATE TABLE IF NOT EXISTS Cards (
    Id               INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BoardId          INT          NOT NULL,
    ColumnId         INT          NOT NULL,
    Title            VARCHAR(500) NOT NULL,
    Description      TEXT             NULL,
    Position         INT          NOT NULL DEFAULT 0,
    Version          INT          NOT NULL DEFAULT 1,
    Priority         TINYINT      NOT NULL DEFAULT 1  COMMENT '0=Low 1=Medium 2=High 3=Critical',
    AssignedToUserId VARCHAR(128)     NULL,
    Metadata         JSON             NULL,
    UpdatedAtUtc     DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
    UpdatedByUserId  VARCHAR(128)     NULL,

    CONSTRAINT fk_cards_board  FOREIGN KEY (BoardId)  REFERENCES Boards(Id)  ON DELETE CASCADE,
    CONSTRAINT fk_cards_column FOREIGN KEY (ColumnId) REFERENCES Columns(Id) ON DELETE CASCADE,

    INDEX idx_cards_board       (BoardId),
    INDEX idx_cards_column_pos  (ColumnId, Position),
    INDEX idx_cards_updated     (UpdatedAtUtc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
