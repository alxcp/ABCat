using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ABCat.Matching.Embedding
{
    /// <summary>
    ///     Runs multilingual-e5-small (ONNX) in-process to turn short texts into L2-normalized
    ///     384-dim sentence embeddings. Mean-pools the last hidden state over the attention mask.
    ///     e5 expects an instruction prefix ("query: " / "passage: "); for symmetric
    ///     similarity/classification use "query: " on both sides.
    /// </summary>
    public sealed class E5Embedder : IDisposable
    {
        public const int Dim = 384;

        private readonly InferenceSession _session;
        private readonly E5Tokenizer _tokenizer;
        private readonly bool _needTypeIds;

        public E5Embedder(string modelPath, string tokenizerJsonPath)
        {
            var opts = new SessionOptions { IntraOpNumThreads = Environment.ProcessorCount };
            _session = new InferenceSession(modelPath, opts);
            _tokenizer = E5Tokenizer.FromTokenizerJson(tokenizerJsonPath);
            _needTypeIds = _session.InputMetadata.ContainsKey("token_type_ids");
        }

        /// <summary>Embed a batch of already-prefixed texts. Returns one float[Dim] per input.</summary>
        public float[][] Embed(IReadOnlyList<string> texts)
        {
            var batch = texts.Count;
            var encoded = new int[batch][];
            var maxLen = 1;
            for (var i = 0; i < batch; i++)
            {
                encoded[i] = _tokenizer.Encode(texts[i]);
                if (encoded[i].Length > maxLen) maxLen = encoded[i].Length;
            }

            var ids = new DenseTensor<long>(new[] { batch, maxLen });
            var mask = new DenseTensor<long>(new[] { batch, maxLen });
            var types = _needTypeIds ? new DenseTensor<long>(new[] { batch, maxLen }) : null;

            for (var i = 0; i < batch; i++)
            {
                var row = encoded[i];
                for (var j = 0; j < row.Length; j++)
                {
                    ids[i, j] = row[j];
                    mask[i, j] = 1;
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", ids),
                NamedOnnxValue.CreateFromTensor("attention_mask", mask)
            };
            if (_needTypeIds)
                inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", types));

            using var results = _session.Run(inputs);
            var hidden = results[0].AsTensor<float>(); // [batch, seq, Dim]

            var output = new float[batch][];
            for (var i = 0; i < batch; i++)
            {
                var vec = new float[Dim];
                var count = 0;
                for (var j = 0; j < maxLen; j++)
                {
                    if (mask[i, j] == 0) continue;
                    count++;
                    for (var d = 0; d < Dim; d++) vec[d] += hidden[i, j, d];
                }

                if (count > 0)
                    for (var d = 0; d < Dim; d++) vec[d] /= count;

                Normalize(vec);
                output[i] = vec;
            }

            return output;
        }

        public float[] EmbedOne(string text) => Embed(new[] { text })[0];

        public static float Cosine(float[] a, float[] b)
        {
            // Vectors are L2-normalized, so cosine is just the dot product.
            float dot = 0;
            for (var i = 0; i < a.Length; i++) dot += a[i] * b[i];
            return dot;
        }

        private static void Normalize(float[] v)
        {
            double sum = 0;
            foreach (var x in v) sum += (double) x * x;
            var norm = (float) Math.Sqrt(sum);
            if (norm <= 1e-8f) return;
            for (var i = 0; i < v.Length; i++) v[i] /= norm;
        }

        public void Dispose() => _session?.Dispose();
    }
}
