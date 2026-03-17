-- Auth0 user IDs look like 'auth0|64abc123' or 'google-oauth2|123456'.
-- Synced from JWT claims on first login via POST /api/me/sync.
CREATE TABLE IF NOT EXISTS Users (
    Id          VARCHAR(128) NOT NULL PRIMARY KEY,
    DisplayName VARCHAR(200) NOT NULL,
    Email       VARCHAR(320)     NULL,
    AvatarUrl   VARCHAR(512)     NULL,
    CreatedAt   DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
    LastSeenAt  DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
