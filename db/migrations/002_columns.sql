CREATE TABLE IF NOT EXISTS Columns (
    Id       INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BoardId  INT          NOT NULL,
    Title    VARCHAR(100) NOT NULL,
    Color    VARCHAR(20)  NOT NULL DEFAULT '#3b82f6',
    Position INT          NOT NULL DEFAULT 0,

    CONSTRAINT fk_columns_board FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE,
    INDEX idx_columns_board_pos (BoardId, Position)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
