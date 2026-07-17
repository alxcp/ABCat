namespace ABCat.ReferenceCatalog
{
    /// <summary>
    ///     DDL for the local reference-catalog SQLite database built from the Flibusta dumps.
    ///     Table/column names are normalized (snake_case, English) and decoupled from the
    ///     upstream MySQL column names; the original Flibusta ids are preserved as-is so the
    ///     matcher can persist them back into ABCat records once a match is confirmed.
    /// </summary>
    internal static class ReferenceSchema
    {
        public const string CreateTables = @"
CREATE TABLE IF NOT EXISTS book (
    book_id   INTEGER PRIMARY KEY,
    title     TEXT NOT NULL,
    title_alt TEXT,
    lang      TEXT,
    year      INTEGER,
    file_type TEXT,
    file_size INTEGER,
    deleted   INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS author (
    author_id   INTEGER PRIMARY KEY,
    first_name  TEXT,
    middle_name TEXT,
    last_name   TEXT,
    nick_name   TEXT,
    gender      TEXT
);

CREATE TABLE IF NOT EXISTS book_author (
    book_id   INTEGER NOT NULL,
    author_id INTEGER NOT NULL,
    pos       INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (book_id, author_id)
) WITHOUT ROWID;

CREATE TABLE IF NOT EXISTS genre (
    genre_id INTEGER PRIMARY KEY,
    code     TEXT NOT NULL,
    descr    TEXT,
    meta     TEXT
);

CREATE TABLE IF NOT EXISTS book_genre (
    book_id  INTEGER NOT NULL,
    genre_id INTEGER NOT NULL,
    PRIMARY KEY (book_id, genre_id)
) WITHOUT ROWID;

CREATE TABLE IF NOT EXISTS sequence (
    seq_id INTEGER PRIMARY KEY,
    name   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS book_sequence (
    book_id INTEGER NOT NULL,
    seq_id  INTEGER NOT NULL,
    seq_num INTEGER,
    PRIMARY KEY (book_id, seq_id)
) WITHOUT ROWID;

-- Known-duplicate map: bad_id (a merged-away record) -> good_id (the surviving one).
-- Ready-made ground truth for training/evaluating the dedup matcher.
CREATE TABLE IF NOT EXISTS joined_book (
    bad_id  INTEGER PRIMARY KEY,
    good_id INTEGER NOT NULL
);

-- Aggregated reader ratings, a popularity prior for tie-breaking matches.
CREATE TABLE IF NOT EXISTS book_rating (
    book_id  INTEGER PRIMARY KEY,
    votes    INTEGER NOT NULL,
    avg_rate REAL NOT NULL
);

-- One book may carry several annotations (different nid / language), so no unique key.
CREATE TABLE IF NOT EXISTS book_annotation (
    book_id INTEGER NOT NULL,
    nid     INTEGER,
    title   TEXT,
    body    TEXT
);

CREATE TABLE IF NOT EXISTS author_annotation (
    author_id INTEGER NOT NULL,
    nid       INTEGER,
    title     TEXT,
    body      TEXT
);

CREATE TABLE IF NOT EXISTS import_meta (
    key   TEXT PRIMARY KEY,
    value TEXT
);
";

        public const string CreateIndexes = @"
CREATE INDEX IF NOT EXISTS ix_book_author_author   ON book_author (author_id);
CREATE INDEX IF NOT EXISTS ix_book_genre_genre     ON book_genre (genre_id);
CREATE INDEX IF NOT EXISTS ix_book_sequence_seq    ON book_sequence (seq_id);
CREATE INDEX IF NOT EXISTS ix_author_last_name     ON author (last_name);
CREATE INDEX IF NOT EXISTS ix_book_title           ON book (title);
CREATE INDEX IF NOT EXISTS ix_book_annotation_book ON book_annotation (book_id);
CREATE INDEX IF NOT EXISTS ix_author_annotation_a  ON author_annotation (author_id);
";
    }
}
