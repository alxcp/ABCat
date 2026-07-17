using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace ABCat.Matching
{
    public sealed class AuthorRec
    {
        public int Id;
        public string Surname;      // original display form
        public string Given;        // original display form
        public string SurnameNorm;
        public string GivenNorm;

        public string Canonical =>
            string.IsNullOrEmpty(Given) ? Surname : Surname + ", " + Given;
    }

    public readonly struct BookRec
    {
        public readonly int Id;
        public readonly string Title;
        public readonly string TitleNorm;

        public BookRec(int id, string title, string titleNorm)
        {
            Id = id;
            Title = title;
            TitleNorm = titleNorm;
        }
    }

    /// <summary>
    ///     In-memory blocking index over the Flibusta reference catalog. Authors are held in
    ///     RAM (bucketed by Russian-metaphone key and by surname prefix) for cheap candidate
    ///     retrieval; a book's editions are fetched from SQLite on demand per candidate author
    ///     (indexed by book_author.author_id) and cached.
    /// </summary>
    public sealed class ReferenceIndex : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly Dictionary<int, AuthorRec> _authors = new Dictionary<int, AuthorRec>();
        private readonly Dictionary<string, List<int>> _byMetaphone = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, List<int>> _byPrefix = new Dictionary<string, List<int>>();
        private readonly Dictionary<int, List<BookRec>> _booksByAuthor = new Dictionary<int, List<BookRec>>();
        private readonly SqliteCommand _booksCmd;

        public ReferenceIndex(string referenceCatalogPath)
        {
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = referenceCatalogPath,
                Mode = SqliteOpenMode.ReadOnly
            };
            _conn = new SqliteConnection(csb.ToString());
            _conn.Open();

            LoadAuthors();

            _booksCmd = _conn.CreateCommand();
            _booksCmd.CommandText =
                "SELECT b.book_id, b.title FROM book b " +
                "JOIN book_author ba ON ba.book_id = b.book_id " +
                "WHERE ba.author_id = @a AND b.deleted = 0";
            _booksParam = _booksCmd.CreateParameter();
            _booksParam.ParameterName = "@a";
            _booksCmd.Parameters.Add(_booksParam);
            _booksCmd.Prepare();
        }

        private readonly SqliteParameter _booksParam;

        public int AuthorCount => _authors.Count;

        public AuthorRec GetAuthor(int id) => _authors.TryGetValue(id, out var a) ? a : null;

        /// <summary>Candidate author ids for a normalized surname (metaphone bucket ∪ prefix bucket).</summary>
        public IEnumerable<int> CandidateAuthors(string surnameNorm)
        {
            if (string.IsNullOrEmpty(surnameNorm)) yield break;

            var seen = new HashSet<int>();

            var mkey = RussianMetaphone.Key(surnameNorm);
            if (_byMetaphone.TryGetValue(mkey, out var byM))
                foreach (var id in byM)
                    if (seen.Add(id))
                        yield return id;

            var pkey = Prefix(surnameNorm);
            if (_byPrefix.TryGetValue(pkey, out var byP))
                foreach (var id in byP)
                    if (seen.Add(id))
                        yield return id;
        }

        public List<BookRec> BooksOf(int authorId)
        {
            if (_booksByAuthor.TryGetValue(authorId, out var cached)) return cached;

            var list = new List<BookRec>();
            _booksParam.Value = authorId;
            using (var r = _booksCmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var id = r.GetInt32(0);
                    var title = r.IsDBNull(1) ? "" : r.GetString(1);
                    list.Add(new BookRec(id, title, TextNormalize.Normalize(title)));
                }
            }

            _booksByAuthor[authorId] = list;
            return list;
        }

        private void LoadAuthors()
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT author_id, first_name, last_name FROM author";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var rec = new AuthorRec
                        {
                            Id = r.GetInt32(0),
                            Given = r.IsDBNull(1) ? "" : r.GetString(1),
                            Surname = r.IsDBNull(2) ? "" : r.GetString(2)
                        };
                        rec.GivenNorm = TextNormalize.Normalize(rec.Given);
                        rec.SurnameNorm = TextNormalize.Normalize(rec.Surname);
                        if (rec.SurnameNorm.Length == 0) continue;

                        _authors[rec.Id] = rec;
                        Bucket(_byMetaphone, RussianMetaphone.Key(rec.SurnameNorm), rec.Id);
                        Bucket(_byPrefix, Prefix(rec.SurnameNorm), rec.Id);
                    }
                }
            }
        }

        private static void Bucket(Dictionary<string, List<int>> map, string key, int id)
        {
            if (key.Length == 0) return;
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<int>();
                map[key] = list;
            }

            list.Add(id);
        }

        private static string Prefix(string surnameNorm)
        {
            return surnameNorm.Length <= 3 ? surnameNorm : surnameNorm.Substring(0, 3);
        }

        public void Dispose()
        {
            _booksCmd?.Dispose();
            _conn?.Dispose();
        }
    }
}
