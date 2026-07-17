using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ABCat.Matching
{
    /// <summary>One overlay row: literary references for a single audiobook.</summary>
    public sealed class NormalizationRow
    {
        public string AudiobookKey;
        public int? AuthorRef;
        public double? AuthorConf;
        public string AuthorCanonical;
        public int? BookRef;
        public double? BookConf;
        public string BookTitle;
        public int? GenreRef;
        public double? GenreConf;
        public string GenreDescr;
        public string MatcherVersion;
        public string MatchedUtc;
    }

    /// <summary>
    ///     The literary-normalization overlay: a standalone SQLite store, keyed by audiobook Key,
    ///     holding independent references into the Flibusta reference catalog (author, book, genre)
    ///     each with its own confidence. Non-destructive — it never touches the catalog records;
    ///     it can be rebuilt or re-scored at any time.
    /// </summary>
    public sealed class NormalizationStore : IDisposable
    {
        private const string Schema = @"
CREATE TABLE IF NOT EXISTS normalization (
    audiobook_key    TEXT PRIMARY KEY,
    author_ref       INTEGER,
    author_conf      REAL,
    author_canonical TEXT,
    book_ref         INTEGER,
    book_conf        REAL,
    book_title       TEXT,
    genre_ref        INTEGER,
    genre_conf       REAL,
    genre_descr      TEXT,
    matcher_version  TEXT,
    matched_utc      TEXT
);";

        private readonly SqliteConnection _conn;

        public NormalizationStore(string dbPath)
        {
            var csb = new SqliteConnectionStringBuilder { DataSource = dbPath };
            _conn = new SqliteConnection(csb.ToString());
            _conn.Open();
            Exec("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
            Exec(Schema);
        }

        /// <summary>Upsert a batch of literary matches inside a single transaction.</summary>
        public void UpsertBatch(IReadOnlyList<(string key, LiteraryMatch match)> items, string matcherVersion)
        {
            var utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            using (var tx = _conn.BeginTransaction())
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO normalization
    (audiobook_key, author_ref, author_conf, author_canonical,
     book_ref, book_conf, book_title, genre_ref, genre_conf, genre_descr,
     matcher_version, matched_utc)
VALUES (@k, @ar, @ac, @acn, @br, @bc, @bt, @gr, @gc, @gd, @mv, @utc)
ON CONFLICT(audiobook_key) DO UPDATE SET
    author_ref=@ar, author_conf=@ac, author_canonical=@acn,
    book_ref=@br, book_conf=@bc, book_title=@bt,
    genre_ref=@gr, genre_conf=@gc, genre_descr=@gd,
    matcher_version=@mv, matched_utc=@utc;";

                var pk = Add(cmd, "@k");
                var par = Add(cmd, "@ar");
                var pac = Add(cmd, "@ac");
                var pacn = Add(cmd, "@acn");
                var pbr = Add(cmd, "@br");
                var pbc = Add(cmd, "@bc");
                var pbt = Add(cmd, "@bt");
                var pgr = Add(cmd, "@gr");
                var pgc = Add(cmd, "@gc");
                var pgd = Add(cmd, "@gd");
                var pmv = Add(cmd, "@mv");
                var putc = Add(cmd, "@utc");
                putc.Value = utc;
                pmv.Value = matcherVersion ?? (object) DBNull.Value;
                cmd.Prepare();

                foreach (var (key, match) in items)
                {
                    if (string.IsNullOrEmpty(key)) continue;

                    pk.Value = key;
                    if (match?.Author is { } a)
                    {
                        par.Value = a.AuthorId;
                        pac.Value = a.Confidence;
                        pacn.Value = a.Canonical ?? (object) DBNull.Value;
                    }
                    else
                    {
                        par.Value = DBNull.Value;
                        pac.Value = DBNull.Value;
                        pacn.Value = DBNull.Value;
                    }

                    if (match?.Book is { } b)
                    {
                        pbr.Value = b.BookId;
                        pbc.Value = b.Confidence;
                        pbt.Value = b.Title ?? (object) DBNull.Value;
                    }
                    else
                    {
                        pbr.Value = DBNull.Value;
                        pbc.Value = DBNull.Value;
                        pbt.Value = DBNull.Value;
                    }

                    // Genre references are not produced yet (embedding stage is separate).
                    pgr.Value = DBNull.Value;
                    pgc.Value = DBNull.Value;
                    pgd.Value = DBNull.Value;

                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }

        public bool TryGet(string audiobookKey, out NormalizationRow row)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT author_ref, author_conf, author_canonical, book_ref, book_conf, book_title, " +
                    "genre_ref, genre_conf, genre_descr, matcher_version, matched_utc " +
                    "FROM normalization WHERE audiobook_key = @k";
                Add(cmd, "@k").Value = audiobookKey;
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) { row = null; return false; }
                    row = new NormalizationRow
                    {
                        AudiobookKey = audiobookKey,
                        AuthorRef = r.IsDBNull(0) ? (int?) null : r.GetInt32(0),
                        AuthorConf = r.IsDBNull(1) ? (double?) null : r.GetDouble(1),
                        AuthorCanonical = r.IsDBNull(2) ? null : r.GetString(2),
                        BookRef = r.IsDBNull(3) ? (int?) null : r.GetInt32(3),
                        BookConf = r.IsDBNull(4) ? (double?) null : r.GetDouble(4),
                        BookTitle = r.IsDBNull(5) ? null : r.GetString(5),
                        GenreRef = r.IsDBNull(6) ? (int?) null : r.GetInt32(6),
                        GenreConf = r.IsDBNull(7) ? (double?) null : r.GetDouble(7),
                        GenreDescr = r.IsDBNull(8) ? null : r.GetString(8),
                        MatcherVersion = r.IsDBNull(9) ? null : r.GetString(9),
                        MatchedUtc = r.IsDBNull(10) ? null : r.GetString(10)
                    };
                    return true;
                }
            }
        }

        public long Count()
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM normalization";
                return (long) cmd.ExecuteScalar();
            }
        }

        private static SqliteParameter Add(SqliteCommand cmd, string name)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            cmd.Parameters.Add(p);
            return p;
        }

        private void Exec(string sql)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose() => _conn?.Dispose();
    }
}
