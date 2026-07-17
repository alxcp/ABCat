using System;
using System.Collections.Generic;
using System.Linq;

namespace ABCat.Matching
{
    /// <summary>
    ///     Alphabet-agnostic fuzzy string scores (0..1) operating on ALREADY-normalized input
    ///     (call <see cref="TextNormalize.Normalize" /> first). Hand-rolled to avoid the
    ///     English-centric preprocessor of FuzzyWuzzy-style libraries mangling Cyrillic.
    /// </summary>
    public static class Fuzz
    {
        /// <summary>Levenshtein similarity ratio, 0..1.</summary>
        public static double Ratio(string a, string b)
        {
            if (a.Length == 0 && b.Length == 0) return 1.0;
            if (a.Length == 0 || b.Length == 0) return 0.0;
            var dist = Levenshtein(a, b);
            var max = Math.Max(a.Length, b.Length);
            return 1.0 - (double) dist / max;
        }

        /// <summary>Order-insensitive: sort tokens, then ratio. "кинг стивен" vs "стивен кинг" → 1.</summary>
        public static double TokenSortRatio(string a, string b)
        {
            return Ratio(SortTokens(a), SortTokens(b));
        }

        /// <summary>
        ///     token_set_ratio: compares the shared token set against each side's remainder;
        ///     robust to word order AND to one side carrying extra tokens.
        /// </summary>
        public static double TokenSetRatio(string a, string b)
        {
            var ta = new SortedSet<string>(a.Split(' ').Where(t => t.Length > 0));
            var tb = new SortedSet<string>(b.Split(' ').Where(t => t.Length > 0));
            if (ta.Count == 0 && tb.Count == 0) return 1.0;

            var inter = new SortedSet<string>(ta);
            inter.IntersectWith(tb);
            var interStr = string.Join(" ", inter);

            var restA = new SortedSet<string>(ta);
            restA.ExceptWith(inter);
            var restB = new SortedSet<string>(tb);
            restB.ExceptWith(inter);

            var combinedA = (interStr + " " + string.Join(" ", restA)).Trim();
            var combinedB = (interStr + " " + string.Join(" ", restB)).Trim();

            var s1 = Ratio(interStr, combinedA);
            var s2 = Ratio(interStr, combinedB);
            var s3 = Ratio(combinedA, combinedB);
            return Math.Max(s1, Math.Max(s2, s3));
        }

        /// <summary>
        ///     Jaro-Winkler, prefix-weighted — the right choice for isolated surnames
        ///     (tolerant of differing endings: "толкин"/"толкиен").
        /// </summary>
        public static double JaroWinkler(string s1, string s2, double p = 0.1)
        {
            var jaro = Jaro(s1, s2);
            var prefix = 0;
            var max = Math.Min(4, Math.Min(s1.Length, s2.Length));
            for (var i = 0; i < max; i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + prefix * p * (1 - jaro);
        }

        private static double Jaro(string s1, string s2)
        {
            if (s1.Length == 0 && s2.Length == 0) return 1.0;
            if (s1.Length == 0 || s2.Length == 0) return 0.0;

            var matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
            if (matchDistance < 0) matchDistance = 0;

            var s1Matches = new bool[s1.Length];
            var s2Matches = new bool[s2.Length];
            var matches = 0;

            for (var i = 0; i < s1.Length; i++)
            {
                var start = Math.Max(0, i - matchDistance);
                var end = Math.Min(i + matchDistance + 1, s2.Length);
                for (var j = start; j < end; j++)
                {
                    if (s2Matches[j] || s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            double t = 0;
            var k = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) t++;
                k++;
            }

            t /= 2;
            return (matches / (double) s1.Length + matches / (double) s2.Length + (matches - t) / matches) / 3;
        }

        private static string SortTokens(string s)
        {
            var tokens = s.Split(' ').Where(t => t.Length > 0).ToArray();
            Array.Sort(tokens, StringComparer.Ordinal);
            return string.Join(" ", tokens);
        }

        private static int Levenshtein(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var prev = new int[m + 1];
            var curr = new int[m + 1];
            for (var j = 0; j <= m; j++) prev[j] = j;

            for (var i = 1; i <= n; i++)
            {
                curr[0] = i;
                var sc = s[i - 1];
                for (var j = 1; j <= m; j++)
                {
                    var cost = sc == t[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(prev[j] + 1, curr[j - 1] + 1), prev[j - 1] + cost);
                }

                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            return prev[m];
        }
    }
}
