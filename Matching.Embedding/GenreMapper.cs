using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Matching;
using Microsoft.Data.Sqlite;

namespace ABCat.Matching.Embedding
{
    /// <summary>
    ///     Maps free-text catalog genre strings onto the fixed FB2 genre taxonomy (the ~270
    ///     <c>genre</c> rows of the Flibusta reference catalog) by nearest embedding. e5 is used
    ///     symmetrically ("query: " prefix on both the FB2 label and the incoming genre atom).
    /// </summary>
    public sealed class GenreMapper
    {
        public sealed class Label
        {
            public int GenreId;
            public string Code;
            public string Descr;
            public string Meta;
            public float[] Vec;
        }

        private readonly E5Embedder _emb;
        private readonly List<Label> _labels = new List<Label>();

        public GenreMapper(string referenceCatalogPath, E5Embedder emb)
        {
            _emb = emb;

            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = referenceCatalogPath,
                Mode = SqliteOpenMode.ReadOnly
            };
            using (var conn = new SqliteConnection(csb.ToString()))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT genre_id, code, descr, meta FROM genre WHERE TRIM(descr) <> ''";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    _labels.Add(new Label
                    {
                        GenreId = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Descr = r.IsDBNull(2) ? "" : r.GetString(2),
                        Meta = r.IsDBNull(3) ? "" : r.GetString(3)
                    });
            }

            var vecs = _emb.Embed(_labels.Select(l => Prefix(l.Descr)).ToList());
            for (var i = 0; i < _labels.Count; i++) _labels[i].Vec = vecs[i];
        }

        public int LabelCount => _labels.Count;

        /// <summary>Split a raw catalog genre cell into atomic genre strings (comma/slash/semicolon).</summary>
        public static IEnumerable<string> Atomize(string rawGenre)
        {
            if (string.IsNullOrWhiteSpace(rawGenre)) yield break;
            foreach (var part in rawGenre.Split(new[] { ',', ';', '/', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) yield return t;
            }
        }

        /// <summary>Embed a batch of genre atoms (adds the e5 prefix).</summary>
        public float[][] EmbedAtoms(IReadOnlyList<string> atoms) =>
            _emb.Embed(atoms.Select(Prefix).ToList());

        /// <summary>Nearest FB2 label to an already-embedded query vector, by cosine.</summary>
        public (Label label, float score) Nearest(float[] queryVec)
        {
            Label best = null;
            var bestScore = float.NegativeInfinity;
            foreach (var l in _labels)
            {
                var s = E5Embedder.Cosine(queryVec, l.Vec);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = l;
                }
            }

            return (best, bestScore);
        }

        /// <summary>
        ///     Hybrid nearest: take the top-K labels by embedding cosine, then re-rank by adding a
        ///     lexical token-overlap bonus. Fixes cases where e5 over-weights shared modifier words
        ///     ("Современная русская проза" vs "…поэзия"). The reported confidence stays the raw
        ///     cosine of the chosen label.
        /// </summary>
        public (Label label, float score) NearestHybrid(float[] queryVec, string atomText, double lexWeight = 0.15)
        {
            var top = _labels
                .Select(l => (label: l, cos: E5Embedder.Cosine(queryVec, l.Vec)))
                .OrderByDescending(x => x.cos)
                .Take(6)
                .ToList();

            var atomNorm = TextNormalize.Normalize(atomText);
            Label best = null;
            float bestCos = 0;
            var bestBlend = double.NegativeInfinity;
            foreach (var (label, cos) in top)
            {
                var lex = Fuzz.TokenSetRatio(atomNorm, TextNormalize.Normalize(label.Descr));
                var blend = cos + lexWeight * lex;
                if (blend > bestBlend)
                {
                    bestBlend = blend;
                    best = label;
                    bestCos = cos;
                }
            }

            return (best, bestCos);
        }

        private static string Prefix(string text) => "query: " + text;
    }
}
