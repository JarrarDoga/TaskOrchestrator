-- One active invite code per board at a time.
-- Rotating generates a new code; the old row isactive=false so previous members
-- are not affected (membership lives in boardmembers, not here).
CREATE TABLE IF NOT EXISTS boardinvites (
    id              SERIAL       NOT NULL PRIMARY KEY,
    boardid         INT          NOT NULL,
    code            VARCHAR(20)  NOT NULL,
    createdbyuserid VARCHAR(128) NOT NULL,
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    expiresat       TIMESTAMPTZ,
    maxuses         INT,
    timesused       INT          NOT NULL DEFAULT 0,
    isactive        BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT fk_invites_board FOREIGN KEY (boardid) REFERENCES boards(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_invites_code  ON boardinvites (code);
CREATE INDEX        IF NOT EXISTS idx_invites_board ON boardinvites (boardid, isactive);
