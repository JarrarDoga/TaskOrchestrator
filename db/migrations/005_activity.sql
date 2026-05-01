CREATE TABLE IF NOT EXISTS cardactivity (
    id              SERIAL       NOT NULL PRIMARY KEY,
    cardid          INT          NOT NULL,
    boardid         INT          NOT NULL,
    eventtype       SMALLINT     NOT NULL,
    userid          VARCHAR(128),
    userdisplayname VARCHAR(200),
    description     VARCHAR(500) NOT NULL,
    occurredatutc   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_activity_card FOREIGN KEY (cardid) REFERENCES cards(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_activity_card  ON cardactivity (cardid,  occurredatutc);
CREATE INDEX IF NOT EXISTS idx_activity_board ON cardactivity (boardid, occurredatutc);
