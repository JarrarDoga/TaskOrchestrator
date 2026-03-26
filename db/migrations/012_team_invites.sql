CREATE TABLE TeamInvites (
    Token           VARCHAR(32)  NOT NULL PRIMARY KEY,
    TeamId          INT          NOT NULL,
    InviteeEmail    VARCHAR(255) NOT NULL,
    CreatedByUserId VARCHAR(128) NOT NULL,
    CreatedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt       DATETIME     NOT NULL,
    AcceptedAt      DATETIME     NULL,
    CONSTRAINT fk_teaminvites_team
        FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_teaminvites_email ON TeamInvites (InviteeEmail);
