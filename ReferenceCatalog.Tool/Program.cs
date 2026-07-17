using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ABCat.ReferenceCatalog;

// Usage: flibusta-import [dumpDir] [outputDb]
// Defaults are the repo's DataBases folder (gitignored) next to the dumps.
var dumpDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DataBases");
dumpDir = Path.GetFullPath(dumpDir);

var dbPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(dumpDir, "reference-catalog.sqlite");

Console.OutputEncoding = Encoding.UTF8;

if (!Directory.Exists(dumpDir))
{
    Console.Error.WriteLine($"Dump directory not found: {dumpDir}");
    return 1;
}

Console.WriteLine($"Source : {dumpDir}");
Console.WriteLine($"Target : {dbPath}");
Console.WriteLine();

var sw = Stopwatch.StartNew();
var importer = new FlibustaImporter(dumpDir, msg => Console.WriteLine($"[{sw.Elapsed:mm\\:ss}] {msg}"));

try
{
    var stats = importer.Import(dbPath);
    sw.Stop();

    Console.WriteLine();
    Console.WriteLine("=== Import complete ===");
    long total = 0;
    foreach (var kv in stats)
    {
        Console.WriteLine($"  {kv.Key,-18} {kv.Value,12:N0}");
        total += kv.Value;
    }

    var sizeMb = new FileInfo(dbPath).Length / 1024.0 / 1024.0;
    Console.WriteLine($"  {"TOTAL rows",-18} {total,12:N0}");
    Console.WriteLine($"  {"DB size",-18} {sizeMb,10:N1} MB");
    Console.WriteLine($"  {"Elapsed",-18} {sw.Elapsed,12:mm\\:ss}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("IMPORT FAILED:");
    Console.Error.WriteLine(ex);
    return 2;
}
