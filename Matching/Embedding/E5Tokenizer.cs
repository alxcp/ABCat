using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ABCat.Matching.Embedding
{
    /// <summary>
    ///     Tokenizer for the multilingual-e5-small (XLM-RoBERTa) model, reimplemented directly
    ///     from its HuggingFace <c>tokenizer.json</c>: a SentencePiece <b>Unigram</b> model whose
    ///     ids are simply the vocab indices, so there is no fairseq-offset ambiguity — exact
    ///     parity with the reference tokenizer without any native dependency.
    ///
    ///     Pipeline: NFKC normalize (approximating the Precompiled charsmap) → Metaspace
    ///     (spaces → ▁, prepend ▁) → Unigram Viterbi best-segmentation → wrap [&lt;s&gt; … &lt;/s&gt;].
    /// </summary>
    public sealed class E5Tokenizer
    {
        private const char Meta = '▁'; // ▁ LOWER ONE EIGHTH BLOCK
        private const int BosId = 0;        // <s>
        private const int EosId = 2;        // </s>
        private const int UnkId = 3;        // <unk>

        private readonly Dictionary<string, (int id, float score)> _pieces;
        private readonly int _maxPieceLen;
        private readonly double _unkPenalty;

        private E5Tokenizer(Dictionary<string, (int, float)> pieces, int maxPieceLen, float minScore)
        {
            _pieces = pieces;
            _maxPieceLen = maxPieceLen;
            _unkPenalty = minScore - 10.0;
        }

        public static E5Tokenizer FromTokenizerJson(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
            var vocab = doc.RootElement.GetProperty("model").GetProperty("vocab");

            var pieces = new Dictionary<string, (int, float)>(vocab.GetArrayLength(), StringComparer.Ordinal);
            var maxLen = 1;
            var minScore = 0f;
            var id = 0;
            foreach (var entry in vocab.EnumerateArray())
            {
                var piece = entry[0].GetString();
                var score = (float) entry[1].GetDouble();
                if (!string.IsNullOrEmpty(piece) && !pieces.ContainsKey(piece))
                {
                    pieces[piece] = (id, score);
                    if (piece.Length > maxLen) maxLen = piece.Length;
                    if (score < minScore) minScore = score;
                }

                id++;
            }

            return new E5Tokenizer(pieces, maxLen, minScore);
        }

        /// <summary>Encode one text into input ids (already wrapped with &lt;s&gt; … &lt;/s&gt;).</summary>
        public int[] Encode(string text)
        {
            var norm = (text ?? string.Empty).Normalize(NormalizationForm.FormKC);

            // Metaspace: replace spaces with ▁ and prepend a leading ▁.
            var sb = new StringBuilder(norm.Length + 1);
            sb.Append(Meta);
            foreach (var c in norm.Trim())
                sb.Append(char.IsWhiteSpace(c) ? Meta : c);
            var s = sb.ToString();

            var ids = Viterbi(s);

            var result = new int[ids.Count + 2];
            result[0] = BosId;
            ids.CopyTo(result, 1);
            result[result.Length - 1] = EosId;
            return result;
        }

        private List<int> Viterbi(string s)
        {
            var n = s.Length;
            var best = new double[n + 1];
            var backId = new int[n + 1];
            var backFrom = new int[n + 1];
            for (var i = 1; i <= n; i++) best[i] = double.NegativeInfinity;

            for (var i = 0; i < n; i++)
            {
                if (double.IsNegativeInfinity(best[i])) continue;

                var matched = false;
                var maxL = Math.Min(_maxPieceLen, n - i);
                for (var len = maxL; len >= 1; len--)
                {
                    var sub = s.Substring(i, len);
                    if (!_pieces.TryGetValue(sub, out var pv)) continue;
                    matched = true;
                    var cand = best[i] + pv.score;
                    if (cand > best[i + len])
                    {
                        best[i + len] = cand;
                        backId[i + len] = pv.id;
                        backFrom[i + len] = i;
                    }
                }

                if (!matched)
                {
                    // Unknown single character → <unk>.
                    var cand = best[i] + _unkPenalty;
                    if (cand > best[i + 1])
                    {
                        best[i + 1] = cand;
                        backId[i + 1] = UnkId;
                        backFrom[i + 1] = i;
                    }
                }
            }

            var ids = new List<int>();
            var pos = n;
            while (pos > 0)
            {
                ids.Add(backId[pos]);
                pos = backFrom[pos];
            }

            ids.Reverse();
            return ids;
        }
    }
}
