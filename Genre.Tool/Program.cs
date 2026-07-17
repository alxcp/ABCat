using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ABCat.Matching.Embedding;
using Microsoft.Data.Sqlite;

// Usage: genre-map [catalogDb] [referenceDb] [modelDir]
// Read-only: maps distinct catalog genre atoms onto FB2 codes via e5 embeddings.

var catalog = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ABCat", "DataBases", "AudioBooks.sqlite");
var reference = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "DataBases", "reference-catalog.sqlite"));
var modelDir = args.Length > 2
    ? args[2]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models", "e5-small"));

Console.OutputEncoding = Encoding.UTF8;
var modelPath = Path.Combine(modelDir, "model.onnx");
var tokPath = Path.Combine(modelDir, "tokenizer.json");
foreach (var p in new[] { catalog, reference, modelPath, tokPath })
    if (!File.Exists(p)) { Console.Error.WriteLine($"Missing: {p}"); return 1; }

var sw = Stopwatch.StartNew();
using var embedder = new E5Embedder(modelPath, tokPath);
Console.WriteLine($"Model loaded in {sw.ElapsedMilliseconds:N0} ms.");

// --- 1) Tokenizer/embedding sanity: synonyms should score far above unrelated pairs. ---
Console.WriteLine("\n=== Embedding sanity (cosine) ===");
string[][] probes =
{
    new[] { "query: фантастика", "query: научная фантастика" },
    new[] { "query: детектив", "query: криминальный роман" },
    new[] { "query: фэнтези", "query: боевое фэнтези" },
    new[] { "query: сказка", "query: сказки для детей" },
    new[] { "query: фантастика", "query: кулинария" },
    new[] { "query: детектив", "query: поэзия" }
};
foreach (var pair in probes)
{
    var v = embedder.Embed(pair);
    Console.WriteLine($"  {E5Embedder.Cosine(v[0], v[1]):F3}  «{pair[0][7..]}» ~ «{pair[1][7..]}»");
}

// --- 2) Build FB2 label index, map distinct catalog genre atoms. ---
sw.Restart();
var mapper = new GenreMapper(reference, embedder);
Console.WriteLine($"\nFB2 labels: {mapper.LabelCount} embedded in {sw.ElapsedMilliseconds:N0} ms.");

var atoms = LoadDistinctAtoms(catalog);
Console.WriteLine($"Distinct catalog genre atoms: {atoms.Count:N0} (from multi-value split)\n");

sw.Restart();
var results = new List<(string atom, int count, GenreMapper.Label label, float score)>(atoms.Count);
const int batch = 256;
var keys = atoms.Keys.ToList();
for (var i = 0; i < keys.Count; i += batch)
{
    var chunk = keys.Skip(i).Take(batch).ToList();
    var vecs = mapper.EmbedAtoms(chunk);
    for (var j = 0; j < chunk.Count; j++)
    {
        // Plain embedding nearest is the honest baseline; NearestHybrid (lexical rerank) is
        // available for the curation stage but needs a better head-noun signal than token_set.
        var (label, score) = mapper.Nearest(vecs[j]);
        results.Add((chunk[j], atoms[chunk[j]], label, score));
    }
}

sw.Stop();
Console.WriteLine($"Mapped {results.Count:N0} atoms in {sw.Elapsed.TotalSeconds:F1} s " +
                  $"({1000.0 * sw.ElapsedMilliseconds / Math.Max(1, results.Count):F1} ms/atom).\n");

// Weight coverage by occurrence count (how many catalog cells are covered).
long total = results.Sum(r => (long)r.count);
long ge85 = results.Where(r => r.score >= 0.85f).Sum(r => (long)r.count);
long ge90 = results.Where(r => r.score >= 0.90f).Sum(r => (long)r.count);
Console.WriteLine("=== Coverage (occurrence-weighted) ===");
Console.WriteLine($"  atoms mapped >=0.85 : {100.0 * ge85 / total:F1}% of occurrences");
Console.WriteLine($"  atoms mapped >=0.90 : {100.0 * ge90 / total:F1}% of occurrences");

Console.WriteLine("\n=== Top-30 atoms by frequency -> FB2 ===");
foreach (var r in results.OrderByDescending(r => r.count).Take(30))
    Console.WriteLine($"  {r.count,5} {r.score:F2}  «{Trim(r.atom,30)}» -> {r.label.Descr} [{r.label.Meta}]");

Console.WriteLine("\n=== 25 random mid-confidence (0.80-0.88) — the interesting zone ===");
var rng = new Random(1);
foreach (var r in results.Where(r => r.score is >= 0.80f and < 0.88f).OrderBy(_ => rng.Next()).Take(25))
    Console.WriteLine($"  {r.score:F2}  «{Trim(r.atom,32)}» -> {r.label.Descr} [{r.label.Meta}]");

Console.WriteLine("\n=== 20 lowest-confidence atoms (likely no FB2 equivalent) ===");
foreach (var r in results.Where(r => r.count >= 3).OrderBy(r => r.score).Take(20))
    Console.WriteLine($"  {r.score:F2}  «{Trim(r.atom,32)}» -> {r.label.Descr}");

return 0;

static Dictionary<string, int> LoadDistinctAtoms(string db)
{
    var snapshot = Path.Combine(Path.GetTempPath(), "abcat-catalog-genre-snap.sqlite");
    File.Copy(db, snapshot, true);
    var csb = new SqliteConnectionStringBuilder { DataSource = snapshot, Mode = SqliteOpenMode.ReadOnly };
    using var conn = new SqliteConnection(csb.ToString());
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Genre FROM AudioBook WHERE TRIM(Genre) <> ''";
    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    using var r = cmd.ExecuteReader();
    while (r.Read())
    {
        var raw = r.IsDBNull(0) ? "" : r.GetString(0);
        foreach (var atom in GenreMapper.Atomize(raw))
        {
            counts.TryGetValue(atom, out var c);
            counts[atom] = c + 1;
        }
    }

    return counts;
}

static string Trim(string s, int max)
{
    s = (s ?? "").Replace("\n", " ").Trim();
    return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
}
