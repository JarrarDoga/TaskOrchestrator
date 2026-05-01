CREATE TABLE IF NOT EXISTS boardmembers (
    boardid  INT          NOT NULL,
    userid   VARCHAR(128) NOT NULL,
    role     VARCHAR(20)  NOT NULL DEFAULT 'Member',
    joinedat TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    PRIMARY KEY (boardid, userid),
    CONSTRAINT fk_members_board FOREIGN KEY (boardid) REFERENCES boards(id) ON DELETE CASCADE,
    CONSTRAINT fk_members_user  FOREIGN KEY (userid)  REFERENCES users(id)  ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_members_user ON boardmembers (userid);
