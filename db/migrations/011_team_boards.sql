-- Link boards to teams (optional) so ownership and access can be modeled at org level.
ALTER TABLE Boards
    ADD COLUMN TeamId INT NULL AFTER Description;

ALTER TABLE Boards
    ADD INDEX idx_boards_team (TeamId);

ALTER TABLE Boards
    ADD CONSTRAINT fk_boards_team
    FOREIGN KEY (TeamId) REFERENCES Teams(Id)
    ON DELETE SET NULL;
