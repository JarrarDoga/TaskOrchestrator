-- File content lives on disk (or S3/Blob). Only metadata here.
CREATE TABLE IF NOT EXISTS attachments (
    id               SERIAL       NOT NULL PRIMARY KEY,
    cardid           INT          NOT NULL,
    filename         VARCHAR(255) NOT NULL,
    contenttype      VARCHAR(100) NOT NULL,
    filesizebytes    BIGINT       NOT NULL DEFAULT 0,
    storagepath      VARCHAR(512) NOT NULL,
    uploadedatutc    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    uploadedbyuserid VARCHAR(128),

    CONSTRAINT fk_attachments_card FOREIGN KEY (cardid) REFERENCES cards(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_attachments_card ON attachments (cardid);
