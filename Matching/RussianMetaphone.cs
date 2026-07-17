using System.Text;

namespace ABCat.Matching
{
    /// <summary>
    ///     Compact phonetic key for Russian surnames (after Kankowski's Russian Metaphone):
    ///     collapses vowels by reduction, unvoices terminal/paired consonants, folds
    ///     phonetically equal letters. Used only as a coarse BLOCKING key so that
    ///     "стивн"/"стивен" and "кинг"/"кинк" land in the same candidate bucket; the fine
    ///     scoring is done by <see cref="Fuzz" />.
    /// </summary>
    public static class RussianMetaphone
    {
        public static string Key(string surnameNormalized)
        {
            if (string.IsNullOrEmpty(surnameNormalized)) return string.Empty;

            var s = surnameNormalized;
            var sb = new StringBuilder(s.Length);

            // 1) vowel reduction: о→а, я→а, е/э→и, ё→и (already е after Normalize), ю→у
            foreach (var raw in s)
            {
                var c = raw;
                switch (c)
                {
                    case 'о': c = 'а'; break;
                    case 'я': c = 'а'; break;
                    case 'е': c = 'и'; break;
                    case 'э': c = 'и'; break;
                    case 'ю': c = 'у'; break;
                }

                sb.Append(c);
            }

            // 2) unvoice paired consonants
            var chars = sb.ToString().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                switch (chars[i])
                {
                    case 'б': chars[i] = 'п'; break;
                    case 'з': chars[i] = 'с'; break;
                    case 'д': chars[i] = 'т'; break;
                    case 'в': chars[i] = 'ф'; break;
                    case 'г': chars[i] = 'к'; break;
                    // ж→ш kept: both map to ш
                    case 'ж': chars[i] = 'ш'; break;
                }
            }

            // 3) drop soft/hard signs, collapse doubled letters
            var outSb = new StringBuilder(chars.Length);
            var prev = '\0';
            foreach (var c in chars)
            {
                if (c == 'ь' || c == 'ъ') continue;
                if (c == prev) continue; // collapse repeats
                if (c == ' ') continue;
                outSb.Append(c);
                prev = c;
            }

            return outSb.ToString();
        }
    }
}
