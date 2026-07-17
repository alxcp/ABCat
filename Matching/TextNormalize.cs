using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABCat.Matching
{
    /// <summary>
    ///     Mechanical text normalization shared by every matcher stage: case folding,
    ///     ё→е, punctuation to spaces, whitespace collapse. Plus Russian-specific helpers
    ///     for splitting a noisy author string and cleaning a "Author - Title" record title.
    /// </summary>
    public static class TextNormalize
    {
        // Both the ASCII hyphen and the en/em dashes are used as "author - title" separators.
        private static readonly string[] TitleSeparators = { " - ", " – ", " — ", " -- " };

        /// <summary>Lowercase, ё→е, strip punctuation to spaces, collapse whitespace.</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var sb = new StringBuilder(s.Length);
            var prevSpace = false;
            foreach (var raw in s)
            {
                var c = char.ToLowerInvariant(raw);
                if (c == 'ё') c = 'е';

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    prevSpace = false;
                }
                else
                {
                    if (!prevSpace && sb.Length > 0) sb.Append(' ');
                    prevSpace = true;
                }
            }

            return sb.ToString().Trim();
        }

        public static string[] Tokens(string normalized)
        {
            return normalized.Length == 0 ? Array.Empty<string>() : normalized.Split(' ');
        }

        /// <summary>
        ///     Parse a raw author cell into author "slots" (one per real person), each carrying
        ///     one or more alternative (surname, given) readings — because the catalog mixes
        ///     "Surname, Given", "Given Surname" and "Surname Given [Patronymic]" freely. The
        ///     matcher scores every reading and keeps the best per slot.
        /// </summary>
        public static List<List<PersonName>> SplitAuthorSlots(string raw)
        {
            var slots = new List<List<PersonName>>();
            if (string.IsNullOrWhiteSpace(raw)) return slots;

            // ';' '/' '&' always separate distinct people.
            foreach (var hard in raw.Split(new[] { ';', '/', '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var chunk = hard.Trim();
                if (chunk.Length == 0) continue;

                var comma = chunk.IndexOf(',');
                if (comma > 0)
                {
                    var before = chunk.Substring(0, comma).Trim();
                    if (!before.Contains(' '))
                    {
                        // "Surname, Given [Patronymic]" — a single person.
                        var given = chunk.Substring(comma + 1).Replace(',', ' ').Trim();
                        slots.Add(new List<PersonName> { PersonName.From(before, given) });
                        continue;
                    }

                    // Multi-author: "Surname Given, Surname Given, ..." — each part a person.
                    foreach (var part in chunk.Split(','))
                    {
                        var p = part.Trim();
                        if (p.Length > 0) slots.Add(FreeFormReadings(p));
                    }

                    continue;
                }

                slots.Add(FreeFormReadings(chunk));
            }

            return slots;
        }

        /// <summary>Both name-order readings of a "First Last" / "Last First Patronymic" chunk.</summary>
        private static List<PersonName> FreeFormReadings(string raw)
        {
            var tokens = Tokens(Normalize(raw));
            var list = new List<PersonName>();
            if (tokens.Length == 0) return list;
            if (tokens.Length == 1)
            {
                list.Add(PersonName.FromNormalized(tokens[0], string.Empty, raw));
                return list;
            }

            // Reading A: last token is the surname (Given [Patronymic] Surname).
            list.Add(PersonName.FromNormalized(tokens[tokens.Length - 1],
                string.Join(" ", tokens.Take(tokens.Length - 1)), raw));
            // Reading B: first token is the surname (Surname Given [Patronymic]).
            list.Add(PersonName.FromNormalized(tokens[0], string.Join(" ", tokens.Skip(1)), raw));
            return list;
        }

        /// <summary>
        ///     Strip the leading "Author - " part and trailing junk (parenthesised notes,
        ///     ellipsis, surrounding quotes) from a record title, returning the bare work title.
        /// </summary>
        public static string CleanTitle(string rawTitle, string rawAuthor)
        {
            if (string.IsNullOrWhiteSpace(rawTitle)) return string.Empty;

            var title = rawTitle.Trim();

            foreach (var sep in TitleSeparators)
            {
                var idx = title.IndexOf(sep, StringComparison.Ordinal);
                if (idx <= 0) continue;

                var left = title.Substring(0, idx);
                var right = title.Substring(idx + sep.Length);
                if (right.Trim().Length == 0) continue;

                if (LooksLikeAuthor(left, rawAuthor))
                {
                    title = right;
                    break;
                }
            }

            title = StripTrailingBrackets(title);
            title = title.Trim().Trim('"', '«', '»', '\'', '…', ' ', '-', '–', '—');
            return title.Trim();
        }

        private static bool LooksLikeAuthor(string left, string rawAuthor)
        {
            if (string.IsNullOrWhiteSpace(rawAuthor)) return true;
            var leftTokens = new HashSet<string>(Tokens(Normalize(left)));
            if (leftTokens.Count == 0) return false;
            return Tokens(Normalize(rawAuthor)).Any(t => t.Length > 1 && leftTokens.Contains(t));
        }

        private static string StripTrailingBrackets(string s)
        {
            var prev = s;
            while (true)
            {
                var t = prev.TrimEnd(' ', '.', ',');
                var last = t.Length > 0 ? t[t.Length - 1] : '\0';
                var (open, close) = last == ')' ? ('(', ')')
                    : last == ']' ? ('[', ']')
                    : last == '}' ? ('{', '}')
                    : ('\0', '\0');
                if (close == '\0') return t;

                var depth = 0;
                var cut = -1;
                for (var i = t.Length - 1; i >= 0; i--)
                {
                    if (t[i] == close) depth++;
                    else if (t[i] == open)
                    {
                        depth--;
                        if (depth == 0) { cut = i; break; }
                    }
                }

                if (cut <= 0) return t;
                prev = t.Substring(0, cut);
            }
        }
    }

    /// <summary>A person name reduced to normalized surname + given.</summary>
    public readonly struct PersonName
    {
        public readonly string Surname; // normalized
        public readonly string Given;   // normalized (may be empty)
        public readonly string Raw;     // original chunk

        private PersonName(string surname, string given, string raw)
        {
            Surname = surname;
            Given = given;
            Raw = raw;
        }

        public static PersonName From(string surname, string given) =>
            new PersonName(TextNormalize.Normalize(surname), TextNormalize.Normalize(given),
                (surname + ", " + given).Trim());

        public static PersonName FromNormalized(string surnameNorm, string givenNorm, string raw) =>
            new PersonName(surnameNorm, givenNorm, raw);
    }
}
