-- Link boards to teams so ownership and access can be modeled at org level.
ALTER TABLE boards ADD COLUMN IF NOT EXISTS teamid INT;

CREATE INDEX IF NOT EXISTS idx_boards_team ON boards (teamid);

ALTER TABLE boards
    ADD CONSTRAINT fk_boards_team
    FOREIGN KEY (teamid) REFERENCES teams(id)
    ON DELETE SET NULL;
