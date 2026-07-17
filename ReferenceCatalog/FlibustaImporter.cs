using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ABCat.ReferenceCatalog
{
    /// <summary>
    ///     Builds the local SQLite reference catalog from a folder of Flibusta SQL dumps
    ///     (the *.sql.gz files from https://flibusta.is/sql/). Streaming, single pass per
    ///     table, bulk-inserted inside one transaction each.
    /// </summary>
    public sealed class FlibustaImporter
    {
        private readonly string _dumpDir;
        private readonly Action<string> _log;

        public FlibustaImporter(string dumpDir, Action<string> log = null)
        {
            _dumpDir = dumpDir ?? throw new ArgumentNullException(nameof(dumpDir));
            _log = log ?? (_ => { });
        }

        public Dictionary<string, long> Import(string sqliteDbPath)
        {
            if (File.Exists(sqliteDbPath)) File.Delete(sqliteDbPath);

            var stats = new Dictionary<string, long>();
            var csb = new SqliteConnectionStringBuilder { DataSource = sqliteDbPath };

            using (var conn = new SqliteConnection(csb.ToString()))
            {
                conn.Open();
                Exec(conn, "PRAGMA journal_mode=OFF; PRAGMA synchronous=OFF; " +
                           "PRAGMA temp_store=MEMORY; PRAGMA cache_size=-262144;");
                Exec(conn, ReferenceSchema.CreateTables);

                stats["book"] = ImportBooks(conn);
                stats["author"] = ImportAuthors(conn);
                stats["book_author"] = ImportBookAuthor(conn);
                stats["genre"] = ImportGenres(conn);
                stats["book_genre"] = ImportBookGenre(conn);
                stats["sequence"] = ImportSequences(conn);
                stats["book_sequence"] = ImportBookSequence(conn);
                stats["joined_book"] = ImportJoinedBooks(conn);
                stats["book_rating"] = ImportRatings(conn);
                stats["book_annotation"] = ImportAnnotations(conn, "lib.b.annotations.sql.gz",
                    "libbannotations", "book_annotation", "book_id");
                stats["author_annotation"] = ImportAnnotations(conn, "lib.a.annotations.sql.gz",
                    "libaannotations", "author_annotation", "author_id");

                _log("Creating indexes...");
                Exec(conn, ReferenceSchema.CreateIndexes);

                WriteMeta(conn, stats);

                _log("VACUUM...");
                Exec(conn, "PRAGMA journal_mode=DELETE;");
                Exec(conn, "VACUUM;");
            }

            return stats;
        }

        // ---- per-table imports -------------------------------------------------

        private long ImportBooks(SqliteConnection conn)
        {
            // libbook: 0 BookId,1 FileSize,3 Title,4 Title1,5 Lang,8 FileType,10 Year,11 Deleted
            return Bulk(conn, "lib.libbook.sql.gz", "libbook",
                "INSERT OR IGNORE INTO book (book_id,title,title_alt,lang,year,file_type,file_size,deleted) " +
                "VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)", 8,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Str(r, 3);
                    p[2].Value = StrN(r, 4);
                    p[3].Value = StrN(r, 5);
                    p[4].Value = Int(r, 10);
                    p[5].Value = StrN(r, 8);
                    p[6].Value = Int(r, 1);
                    p[7].Value = r.Length > 11 && r[11] == "1" ? 1 : 0;
                });
        }

        private long ImportAuthors(SqliteConnection conn)
        {
            // libavtorname: 0 AvtorId,1 FirstName,2 MiddleName,3 LastName,4 NickName,8 Gender
            return Bulk(conn, "lib.libavtorname.sql.gz", "libavtorname",
                "INSERT OR IGNORE INTO author (author_id,first_name,middle_name,last_name,nick_name,gender) " +
                "VALUES (@p0,@p1,@p2,@p3,@p4,@p5)", 6,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = StrN(r, 1);
                    p[2].Value = StrN(r, 2);
                    p[3].Value = StrN(r, 3);
                    p[4].Value = StrN(r, 4);
                    p[5].Value = StrN(r, 8);
                });
        }

        private long ImportBookAuthor(SqliteConnection conn)
        {
            // libavtor: 0 BookId,1 AvtorId,2 Pos
            return Bulk(conn, "lib.libavtor.sql.gz", "libavtor",
                "INSERT OR IGNORE INTO book_author (book_id,author_id,pos) VALUES (@p0,@p1,@p2)", 3,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Int(r, 1);
                    p[2].Value = Int(r, 2);
                });
        }

        private long ImportGenres(SqliteConnection conn)
        {
            // libgenrelist: 0 GenreId,1 GenreCode,2 GenreDesc,3 GenreMeta
            return Bulk(conn, "lib.libgenrelist.sql.gz", "libgenrelist",
                "INSERT OR IGNORE INTO genre (genre_id,code,descr,meta) VALUES (@p0,@p1,@p2,@p3)", 4,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Str(r, 1);
                    p[2].Value = StrN(r, 2);
                    p[3].Value = StrN(r, 3);
                });
        }

        private long ImportBookGenre(SqliteConnection conn)
        {
            // libgenre: 0 Id,1 BookId,2 GenreId
            return Bulk(conn, "lib.libgenre.sql.gz", "libgenre",
                "INSERT OR IGNORE INTO book_genre (book_id,genre_id) VALUES (@p0,@p1)", 2,
                (p, r) =>
                {
                    p[0].Value = Int(r, 1);
                    p[1].Value = Int(r, 2);
                });
        }

        private long ImportSequences(SqliteConnection conn)
        {
            // libseqname: 0 SeqId,1 SeqName
            return Bulk(conn, "lib.libseqname.sql.gz", "libseqname",
                "INSERT OR IGNORE INTO sequence (seq_id,name) VALUES (@p0,@p1)", 2,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Str(r, 1);
                });
        }

        private long ImportBookSequence(SqliteConnection conn)
        {
            // libseq: 0 BookId,1 SeqId,2 SeqNumb
            return Bulk(conn, "lib.libseq.sql.gz", "libseq",
                "INSERT OR IGNORE INTO book_sequence (book_id,seq_id,seq_num) VALUES (@p0,@p1,@p2)", 3,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Int(r, 1);
                    p[2].Value = Int(r, 2);
                });
        }

        private long ImportJoinedBooks(SqliteConnection conn)
        {
            // libjoinedbooks: 0 Id,1 Time,2 BadId,3 GoodId,4 realId
            return Bulk(conn, "lib.libjoinedbooks.sql.gz", "libjoinedbooks",
                "INSERT OR IGNORE INTO joined_book (bad_id,good_id) VALUES (@p0,@p1)", 2,
                (p, r) =>
                {
                    p[0].Value = Int(r, 2);
                    p[1].Value = Int(r, 3);
                });
        }

        private long ImportRatings(SqliteConnection conn)
        {
            // librate: 0 ID,1 BookId,2 UserId,3 Rate ('1'..'5'). Aggregate in memory.
            _log("Aggregating librate...");
            var votes = new Dictionary<long, int>();
            var sum = new Dictionary<long, long>();

            var file = Path.Combine(_dumpDir, "lib.librate.sql.gz");
            foreach (var r in MySqlDumpReader.ReadRows(File.OpenRead(file), "librate"))
            {
                if (r.Length < 4) continue;
                if (!int.TryParse(r[3], out var rate) || rate < 1 || rate > 5) continue;
                var bookId = Int(r, 1);
                votes.TryGetValue(bookId, out var v);
                votes[bookId] = v + 1;
                sum.TryGetValue(bookId, out var s);
                sum[bookId] = s + rate;
            }

            using (var tx = conn.BeginTransaction())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT OR REPLACE INTO book_rating (book_id,votes,avg_rate) VALUES (@b,@v,@a)";
                var pb = Add(cmd, "@b");
                var pv = Add(cmd, "@v");
                var pa = Add(cmd, "@a");
                cmd.Prepare();

                foreach (var kv in votes)
                {
                    pb.Value = kv.Key;
                    pv.Value = kv.Value;
                    pa.Value = (double) sum[kv.Key] / kv.Value;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }

            _log($"  book_rating: {votes.Count:N0} books");
            return votes.Count;
        }

        private long ImportAnnotations(SqliteConnection conn, string file, string table,
            string targetTable, string idColumn)
        {
            // annotations: 0 (Book|Avtor)Id, 1 nid, 2 Title, 3 Body
            return Bulk(conn, file, table,
                $"INSERT INTO {targetTable} ({idColumn},nid,title,body) VALUES (@p0,@p1,@p2,@p3)", 4,
                (p, r) =>
                {
                    p[0].Value = Int(r, 0);
                    p[1].Value = Int(r, 1);
                    p[2].Value = StrN(r, 2);
                    p[3].Value = StrN(r, 3);
                });
        }

        // ---- infrastructure ----------------------------------------------------

        private long Bulk(SqliteConnection conn, string file, string table, string sql,
            int nparams, Action<SqliteParameter[], string[]> fill)
        {
            var path = Path.Combine(_dumpDir, file);
            if (!File.Exists(path))
            {
                _log($"  {table}: SKIPPED (missing {file})");
                return 0;
            }

            long n = 0;
            using (var tx = conn.BeginTransaction())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                var ps = new SqliteParameter[nparams];
                for (var k = 0; k < nparams; k++) ps[k] = Add(cmd, "@p" + k);
                cmd.Prepare();

                foreach (var row in MySqlDumpReader.ReadRows(File.OpenRead(path), table))
                {
                    fill(ps, row);
                    cmd.ExecuteNonQuery();
                    if (++n % 200000 == 0) _log($"  {table}: {n:N0}...");
                }

                tx.Commit();
            }

            _log($"  {table}: {n:N0}");
            return n;
        }

        private static SqliteParameter Add(SqliteCommand cmd, string name)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            cmd.Parameters.Add(p);
            return p;
        }

        private void WriteMeta(SqliteConnection conn, Dictionary<string, long> stats)
        {
            using (var tx = conn.BeginTransaction())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO import_meta (key,value) VALUES (@k,@v)";
                var pk = Add(cmd, "@k");
                var pv = Add(cmd, "@v");

                void Put(string k, string v)
                {
                    pk.Value = k;
                    pv.Value = v;
                    cmd.ExecuteNonQuery();
                }

                Put("source", "Flibusta SQL dump");
                Put("imported_utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                foreach (var kv in stats)
                    Put("rows_" + kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture));

                tx.Commit();
            }
        }

        private static void Exec(SqliteConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static long Int(string[] row, int idx)
        {
            if (idx >= row.Length) return 0;
            var s = row[idx];
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static string Str(string[] row, int idx) => idx < row.Length ? row[idx] ?? "" : "";

        private static object StrN(string[] row, int idx)
        {
            if (idx >= row.Length || row[idx] == null || row[idx].Length == 0) return DBNull.Value;
            return row[idx];
        }
    }
}
