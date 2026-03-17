-- Task Orchestrator — MariaDB 11.x schema
-- Run once against an empty `task_orchestrator` database.

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS Boards (
    Id          INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(200) NOT NULL,
    Description TEXT             NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    Version     INT          NOT NULL DEFAULT 1,
    INDEX idx_boards_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS TaskItems (
    Id               INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BoardId          INT          NOT NULL,
    Title            VARCHAR(500) NOT NULL,
    Description      TEXT             NULL,
    Status           TINYINT      NOT NULL DEFAULT 0   COMMENT '0=Backlog 1=Todo 2=InProgress 3=Done',
    Priority         TINYINT      NOT NULL DEFAULT 1   COMMENT '0=Low 1=Medium 2=High 3=Critical',
    AssignedToUserId VARCHAR(128)     NULL,
    Position         INT          NOT NULL DEFAULT 0,
    CreatedAt        DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt        DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    Version          INT          NOT NULL DEFAULT 1,

    CONSTRAINT fk_taskitems_board FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE,
    INDEX idx_taskitems_board_status (BoardId, Status, Position)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed a default board so the client has something to show on first run
INSERT INTO Boards (Name, Description) VALUES ('My First Board', 'Default board created by schema seed.');
