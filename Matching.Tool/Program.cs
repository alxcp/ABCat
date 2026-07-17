using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ABCat.Matching;
using Microsoft.Data.Sqlite;

// Usage: match-eval [catalogDb] [referenceDb] [sampleSize|all]
// Read-only evaluation: runs the literary matcher over real catalog records and reports
// coverage + confidence spread + samples. Writes nothing.

var catalog = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ABCat", "DataBases", "AudioBooks.sqlite");

var reference = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "DataBases", "reference-catalog.sqlite"));

var sampleArg = args.Length > 2 ? args[2] : "5000";
var sample = sampleArg.Equals("all", StringComparison.OrdinalIgnoreCase) ? -1 : int.Parse(sampleArg);

Console.OutputEncoding = Encoding.UTF8;
if (!File.Exists(catalog)) { Console.Error.WriteLine($"Catalog not found: {catalog}"); return 1; }
if (!File.Exists(reference)) { Console.Error.WriteLine($"Reference not found: {reference}"); return 1; }

// The catalog may be locked by the running app; work on an immutable snapshot copy.
var snapshot = Path.Combine(Path.GetTempPath(), "abcat-catalog-snapshot.sqlite");
File.Copy(catalog, snapshot, true);

Console.WriteLine($"Catalog  : {catalog}");
Console.WriteLine($"Reference: {reference}");
Console.WriteLine($"Sample   : {(sample < 0 ? "all" : sample.ToString())}");
Console.WriteLine();

var records = LoadRecords(snapshot, sample);
Console.WriteLine($"Loaded {records.Count:N0} records with a non-empty Author.");

var sw = Stopwatch.StartNew();
using var index = new ReferenceIndex(reference);
Console.WriteLine($"Reference index: {index.AuthorCount:N0} authors in {sw.ElapsedMilliseconds:N0} ms.");
Console.WriteLine();

var matcher = new LiteraryMatcher(index);

int authorMatched = 0, authorHi = 0, bookMatched = 0, bookHi = 0;
var aBuckets = new int[11];
var bBuckets = new int[11];
var bothSamples = new List<string>();
var authorOnly = new List<string>();
var misses = new List<string>();

sw.Restart();
foreach (var (author, title) in records)
{
    var m = matcher.Match(author, title);

    if (m.Author is { } a)
    {
        authorMatched++;
        if (a.Confidence >= 0.90) authorHi++;
        aBuckets[Bucket(a.Confidence)]++;

        if (m.Book is { } b)
        {
            bookMatched++;
            if (b.Confidence >= 0.85) bookHi++;
            bBuckets[Bucket(b.Confidence)]++;
            if (bothSamples.Count < 30)
                bothSamples.Add(
                    $"  A {a.Confidence:F2} | B {b.Confidence:F2}  «{Trim(author,22)}» / «{Trim(title,34)}»\n" +
                    $"        -> {a.Canonical}  ::  {Trim(b.Title, 44)}");
        }
        else if (authorOnly.Count < 18)
        {
            authorOnly.Add($"  A {a.Confidence:F2}  «{Trim(author,24)}» -> {a.Canonical}   (title «{Trim(title,34)}»)");
        }
    }
    else if (misses.Count < 18)
    {
        misses.Add($"  «{Trim(author,30)}»  /  «{Trim(title,40)}»");
    }
}

sw.Stop();
var n = records.Count;
Console.WriteLine($"Matched in {sw.Elapsed.TotalSeconds:F1} s ({1000.0 * sw.ElapsedMilliseconds / Math.Max(1, n) / 1000.0:F2} ms/rec avg).\n");

Console.WriteLine("=== Coverage ===");
Pct("author matched (>=0.80)", authorMatched, n);
Pct("  of which conf >=0.90 ", authorHi, n);
Pct("book matched   (>=0.60)", bookMatched, n);
Pct("  of which conf >=0.85 ", bookHi, n);

Console.WriteLine("\n=== Author confidence histogram ===");
Histogram(aBuckets, n);
Console.WriteLine("=== Book confidence histogram ===");
Histogram(bBuckets, n);

Console.WriteLine("\n=== Sample: author + book matched ===");
foreach (var s in bothSamples) Console.WriteLine(s);
Console.WriteLine("\n=== Sample: author matched, no book ===");
foreach (var s in authorOnly) Console.WriteLine(s);
Console.WriteLine("\n=== Sample: author NOT matched ===");
foreach (var s in misses) Console.WriteLine(s);

return 0;

static List<(string author, string title)> LoadRecords(string db, int sample)
{
    var csb = new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly };
    using var conn = new SqliteConnection(csb.ToString());
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Author, Title FROM AudioBook WHERE TRIM(Author) <> ''" +
                      (sample < 0 ? "" : " ORDER BY RANDOM() LIMIT " + sample);
    var list = new List<(string, string)>();
    using var r = cmd.ExecuteReader();
    while (r.Read())
        list.Add((r.IsDBNull(0) ? "" : r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1)));
    return list;
}

static int Bucket(double c)
{
    var b = (int)(c * 10);
    return b < 0 ? 0 : b > 10 ? 10 : b;
}

static void Pct(string label, int k, int n) =>
    Console.WriteLine($"  {label,-26} {k,7:N0}  {(n == 0 ? 0 : 100.0 * k / n),5:F1}%");

static void Histogram(int[] buckets, int n)
{
    for (var i = 10; i >= 6; i--)
    {
        var lo = i / 10.0;
        var bar = new string('#', (int)(60.0 * buckets[i] / Math.Max(1, n)));
        Console.WriteLine($"  {lo:F1}: {buckets[i],6:N0} {bar}");
    }
}

static string Trim(string s, int max)
{
    s = (s ?? "").Replace("\n", " ").Trim();
    return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
}
