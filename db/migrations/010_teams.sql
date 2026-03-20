-- Teams: organizational groupings of users that can share boards
CREATE TABLE Teams (
    Id              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name            VARCHAR(200) NOT NULL,
    Description     TEXT,
    Slug            VARCHAR(100) NOT NULL,
    Icon            VARCHAR(100) NOT NULL DEFAULT 'group',
    IsPublic        TINYINT(1)   NOT NULL DEFAULT 1,
    CreatedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedByUserId VARCHAR(128) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Team membership (many-to-many with role)
-- FK to Users omitted: collation varies by host; integrity enforced at app layer.
CREATE TABLE TeamMembers (
    TeamId   INT          NOT NULL,
    UserId   VARCHAR(128) NOT NULL,
    Role     VARCHAR(20)  NOT NULL DEFAULT 'Member',
    JoinedAt DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (TeamId, UserId),
    CONSTRAINT fk_teammembers_team FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
