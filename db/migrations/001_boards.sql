CREATE TABLE IF NOT EXISTS boards (
    id          SERIAL       NOT NULL PRIMARY KEY,
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    createdat   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    version     INT          NOT NULL DEFAULT 1
);
