CREATE TABLE IF NOT EXISTS teaminvites (
    token           VARCHAR(32)  NOT NULL PRIMARY KEY,
    teamid          INT          NOT NULL,
    inviteeemail    VARCHAR(255) NOT NULL,
    createdbyuserid VARCHAR(128) NOT NULL,
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    expiresat       TIMESTAMPTZ  NOT NULL,
    acceptedat      TIMESTAMPTZ,
    CONSTRAINT fk_teaminvites_team
        FOREIGN KEY (teamid) REFERENCES teams(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_teaminvites_email ON teaminvites (inviteeemail);
