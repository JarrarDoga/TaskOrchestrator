-- One active invite code per board at a time.
-- Rotating generates a new code; the old row IsActive=0 so previous members
-- are not affected (membership lives in BoardMembers, not here).
CREATE TABLE IF NOT EXISTS BoardInvites (
    Id              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BoardId         INT          NOT NULL,
    Code            VARCHAR(20)  NOT NULL,
    CreatedByUserId VARCHAR(128) NOT NULL,
    CreatedAt       DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
    ExpiresAt       DATETIME(3)      NULL,
    MaxUses         INT              NULL,
    TimesUsed       INT          NOT NULL DEFAULT 0,
    IsActive        TINYINT(1)   NOT NULL DEFAULT 1,

    CONSTRAINT fk_invites_board FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE,
    UNIQUE INDEX idx_invites_code   (Code),
    INDEX        idx_invites_board  (BoardId, IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
