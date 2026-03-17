-- Run in order against an empty `task_orchestrator` database.
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS Boards (
    Id          INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(200) NOT NULL,
    Description TEXT             NULL,
    CreatedAt   DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
    Version     INT          NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
