CREATE TABLE IF NOT EXISTS BoardMembers (
    BoardId  INT          NOT NULL,
    UserId   VARCHAR(128) NOT NULL,
    Role     VARCHAR(20)  NOT NULL DEFAULT 'Member' COMMENT 'Owner | Member',
    JoinedAt DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),

    PRIMARY KEY (BoardId, UserId),
    CONSTRAINT fk_members_board FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE,
    CONSTRAINT fk_members_user  FOREIGN KEY (UserId)  REFERENCES Users(Id)  ON DELETE CASCADE,
    INDEX idx_members_user (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
