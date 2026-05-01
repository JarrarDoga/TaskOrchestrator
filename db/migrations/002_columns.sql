CREATE TABLE IF NOT EXISTS columns (
    id       SERIAL       NOT NULL PRIMARY KEY,
    boardid  INT          NOT NULL,
    title    VARCHAR(100) NOT NULL,
    color    VARCHAR(20)  NOT NULL DEFAULT '#3b82f6',
    position INT          NOT NULL DEFAULT 0,

    CONSTRAINT fk_columns_board FOREIGN KEY (boardid) REFERENCES boards(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_columns_board_pos ON columns (boardid, position);
