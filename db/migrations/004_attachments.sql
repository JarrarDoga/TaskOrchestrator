-- File content lives on disk (or eventually S3/Blob). Only metadata here.
CREATE TABLE IF NOT EXISTS Attachments (
    Id                INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    CardId            INT          NOT NULL,
    FileName          VARCHAR(255) NOT NULL,
    ContentType       VARCHAR(100) NOT NULL,
    FileSizeBytes     BIGINT       NOT NULL DEFAULT 0,
    StoragePath       VARCHAR(512) NOT NULL,
    UploadedAtUtc     DATETIME(3)  NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
    UploadedByUserId  VARCHAR(128)     NULL,

    CONSTRAINT fk_attachments_card FOREIGN KEY (CardId) REFERENCES Cards(Id) ON DELETE CASCADE,
    INDEX idx_attachments_card (CardId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
