-- JSONB gives indexable, binary-stored JSON in PostgreSQL.
-- Dapper maps it as string; the repository deserialises it.

CREATE TABLE IF NOT EXISTS cards (
    id               SERIAL       NOT NULL PRIMARY KEY,
    boardid          INT          NOT NULL,
    columnid         INT          NOT NULL,
    title            VARCHAR(500) NOT NULL,
    description      TEXT,
    position         INT          NOT NULL DEFAULT 0,
    version          INT          NOT NULL DEFAULT 1,
    priority         SMALLINT     NOT NULL DEFAULT 1,
    assignedtouserid VARCHAR(128),
    metadata         JSONB,
    updatedatutc     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updatedbyuserid  VARCHAR(128),

    CONSTRAINT fk_cards_board  FOREIGN KEY (boardid)  REFERENCES boards(id)   ON DELETE CASCADE,
    CONSTRAINT fk_cards_column FOREIGN KEY (columnid) REFERENCES columns(id)  ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_cards_board      ON cards (boardid);
CREATE INDEX IF NOT EXISTS idx_cards_column_pos ON cards (columnid, position);
CREATE INDEX IF NOT EXISTS idx_cards_updated    ON cards (updatedatutc);
