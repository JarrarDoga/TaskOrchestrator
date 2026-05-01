CREATE TABLE IF NOT EXISTS teams (
    id              SERIAL       NOT NULL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    description     TEXT,
    slug            VARCHAR(100) NOT NULL,
    icon            VARCHAR(100) NOT NULL DEFAULT 'group',
    ispublic        BOOLEAN      NOT NULL DEFAULT TRUE,
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    createdbyuserid VARCHAR(128) NOT NULL
);

CREATE TABLE IF NOT EXISTS teammembers (
    teamid   INT          NOT NULL,
    userid   VARCHAR(128) NOT NULL,
    role     VARCHAR(20)  NOT NULL DEFAULT 'Member',
    joinedat TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    PRIMARY KEY (teamid, userid),
    CONSTRAINT fk_teammembers_team FOREIGN KEY (teamid) REFERENCES teams(id) ON DELETE CASCADE
);
