using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ABCat.ReferenceCatalog
{
    /// <summary>
    ///     Streaming reader for mysqldump --extended-insert output, as produced by the
    ///     Flibusta SQL exports (https://flibusta.is/sql/, gzip-compressed).
    ///     Yields the value tuples of every <c>INSERT INTO `table` VALUES ...</c> statement
    ///     for a single table as <see cref="string" />[] rows. A SQL NULL comes back as a
    ///     null array element; every other value is returned as its raw (unescaped) text.
    /// </summary>
    public static class MySqlDumpReader
    {
        /// <param name="gzippedSql">A .sql.gz stream. Disposed together with the enumerator.</param>
        /// <param name="tableName">The bare table name, e.g. "libbook".</param>
        public static IEnumerable<string[]> ReadRows(Stream gzippedSql, string tableName)
        {
            var prefix = "INSERT INTO `" + tableName + "` VALUES ";

            using (gzippedSql)
            using (var gz = new GZipStream(gzippedSql, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz, Encoding.UTF8, true, 1 << 20))
            {
                string current = null;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (current == null)
                    {
                        if (!line.StartsWith(prefix, StringComparison.Ordinal))
                            continue;
                        current = line;
                    }
                    else
                    {
                        // Continuation of a statement that carried an (escaped) raw newline.
                        current += "\n" + line;
                    }

                    if (!EndsStatement(line))
                        continue;

                    foreach (var row in ParseValues(current, prefix.Length))
                        yield return row;
                    current = null;
                }

                if (current != null)
                    foreach (var row in ParseValues(current, prefix.Length))
                        yield return row;
            }
        }

        private static bool EndsStatement(string line)
        {
            for (var i = line.Length - 1; i >= 0; i--)
            {
                var c = line[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') continue;
                return c == ';';
            }

            return false;
        }

        private static IEnumerable<string[]> ParseValues(string s, int start)
        {
            var n = s.Length;
            var i = start;
            var sb = new StringBuilder(256);

            while (i < n)
            {
                // Between tuples: skip separators until the next '('.
                while (i < n && s[i] != '(')
                {
                    if (s[i] == ';') { i = n; break; }
                    i++;
                }

                if (i >= n) break;
                i++; // consume '('

                var row = new List<string>(24);
                while (true)
                {
                    while (i < n && s[i] == ' ') i++;
                    if (i >= n) break;

                    var c = s[i];
                    if (c == '\'')
                    {
                        i++;
                        sb.Clear();
                        while (i < n)
                        {
                            var ch = s[i++];
                            if (ch == '\\')
                            {
                                if (i < n) sb.Append(Unescape(s[i++]));
                            }
                            else if (ch == '\'')
                            {
                                if (i < n && s[i] == '\'') { sb.Append('\''); i++; } // doubled ''
                                else break;
                            }
                            else
                            {
                                sb.Append(ch);
                            }
                        }

                        row.Add(sb.ToString());
                    }
                    else if (c == 'N' && i + 4 <= n && string.CompareOrdinal(s, i, "NULL", 0, 4) == 0)
                    {
                        row.Add(null);
                        i += 4;
                    }
                    else
                    {
                        var from = i;
                        while (i < n && s[i] != ',' && s[i] != ')') i++;
                        row.Add(s.Substring(from, i - from).Trim());
                    }

                    while (i < n && s[i] == ' ') i++;
                    if (i < n && s[i] == ',') { i++; continue; }
                    if (i < n && s[i] == ')') { i++; break; }
                    break; // malformed / end of buffer
                }

                yield return row.ToArray();
            }
        }

        private static char Unescape(char c)
        {
            switch (c)
            {
                case '0': return '\0';
                case 'b': return '\b';
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'Z': return (char)0x1A; // MySQL \Z -> SUB
                default: return c; // \\ -> \, \' -> ', \" -> ", etc.
            }
        }
    }
}
