-- Auth0 user IDs look like 'auth0|64abc123' or 'google-oauth2|123456'.
-- Synced from JWT claims on first login via POST /api/me/sync.
CREATE TABLE IF NOT EXISTS users (
    id          VARCHAR(128) NOT NULL PRIMARY KEY,
    displayname VARCHAR(200) NOT NULL,
    email       VARCHAR(320),
    avatarurl   VARCHAR(512),
    createdat   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    lastseenant TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
