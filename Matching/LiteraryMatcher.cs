using System;
using System.Collections.Generic;
using System.Linq;

namespace ABCat.Matching
{
    public readonly struct AuthorRef
    {
        public readonly int AuthorId;
        public readonly string Canonical;
        public readonly double Confidence;

        public AuthorRef(int authorId, string canonical, double confidence)
        {
            AuthorId = authorId;
            Canonical = canonical;
            Confidence = confidence;
        }
    }

    public readonly struct BookRef
    {
        public readonly int BookId;
        public readonly string Title;
        public readonly int AuthorId;
        public readonly double Confidence;

        public BookRef(int bookId, string title, int authorId, double confidence)
        {
            BookId = bookId;
            Title = title;
            AuthorId = authorId;
            Confidence = confidence;
        }
    }

    /// <summary>
    ///     One record's literary-normalization result: independent references into the Flibusta
    ///     catalog, each with its own confidence. Author is matched first (cheap, robust); the
    ///     book is then matched only within the matched authors' works. Either may be null.
    /// </summary>
    public sealed class LiteraryMatch
    {
        public AuthorRef? Author;
        public BookRef? Book;
    }

    /// <summary>
    ///     Matches a noisy catalog record against the Flibusta reference catalog. Stateless per
    ///     call; holds tunable weights/thresholds so the harness can sweep them.
    /// </summary>
    public sealed class LiteraryMatcher
    {
        private readonly ReferenceIndex _index;

        // Tunables (swept from the harness).
        public double SurnameWeight = 0.65;
        public double GivenWeight = 0.35;
        public double AuthorFloor = 0.80;   // below this an author candidate is discarded
        public int AuthorTopK = 4;          // how many top authors feed the book stage
        public double TitleFloor = 0.60;    // below this no book reference is emitted

        public LiteraryMatcher(ReferenceIndex index)
        {
            _index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public LiteraryMatch Match(string rawAuthor, string rawTitle)
        {
            var result = new LiteraryMatch();

            var slots = TextNormalize.SplitAuthorSlots(rawAuthor);
            var rankedAuthors = RankAuthors(slots);
            if (rankedAuthors.Count == 0) return result;

            var best = rankedAuthors[0];
            result.Author = new AuthorRef(best.rec.Id, best.rec.Canonical, Round(best.score));

            var cleanTitle = TextNormalize.Normalize(TextNormalize.CleanTitle(rawTitle, rawAuthor));
            if (cleanTitle.Length == 0) return result;

            BookRef? bestBook = null;
            foreach (var (rec, aScore) in rankedAuthors.Take(AuthorTopK))
            {
                foreach (var book in _index.BooksOf(rec.Id))
                {
                    if (book.TitleNorm.Length == 0) continue;
                    var tScore = Fuzz.TokenSetRatio(cleanTitle, book.TitleNorm);
                    if (tScore < TitleFloor) continue;
                    if (bestBook == null || tScore > bestBook.Value.Confidence)
                        bestBook = new BookRef(book.Id, book.Title, rec.Id, Round(tScore));
                }
            }

            result.Book = bestBook;
            return result;
        }

        private List<(AuthorRec rec, double score)> RankAuthors(List<List<PersonName>> slots)
        {
            var ranked = new List<(AuthorRec, double)>();

            foreach (var readings in slots)
            {
                // Best (candidate, score) across every reading of this one author slot.
                (AuthorRec rec, double score) slotBest = (null, 0);
                foreach (var person in readings)
                {
                    if (person.Surname.Length == 0) continue;
                    foreach (var id in _index.CandidateAuthors(person.Surname))
                    {
                        var cand = _index.GetAuthor(id);
                        if (cand == null) continue;
                        var score = ScoreAuthor(person, cand);
                        if (score > slotBest.score) slotBest = (cand, score);
                    }
                }

                if (slotBest.rec != null && slotBest.score >= AuthorFloor)
                    ranked.Add(slotBest);
            }

            return ranked.OrderByDescending(x => x.Item2).ToList();
        }

        private double ScoreAuthor(PersonName person, AuthorRec cand)
        {
            var surnameScore = Fuzz.JaroWinkler(person.Surname, cand.SurnameNorm);

            // Also allow "Given Surname" free-form to have matched the surname in the given slot:
            // token_set over the whole name catches swapped order and extra patronymics.
            var fullRec = (person.Surname + " " + person.Given).Trim();
            var fullCand = (cand.SurnameNorm + " " + cand.GivenNorm).Trim();
            var tokenSet = Fuzz.TokenSetRatio(fullRec, fullCand);

            double score;
            if (person.Given.Length == 0 || cand.GivenNorm.Length == 0)
            {
                // No given name to confirm on one side: lean on surname, capped a bit.
                score = 0.9 * surnameScore;
            }
            else
            {
                var givenScore = GivenScore(person.Given, cand.GivenNorm);
                score = SurnameWeight * surnameScore + GivenWeight * givenScore;
            }

            return Math.Max(score, tokenSet * 0.98);
        }

        private static double GivenScore(string a, string b)
        {
            // Initials: "а" vs "александр" → compare leading letters only.
            var aInitial = a.Length <= 2;
            var bInitial = b.Length <= 2;
            if (aInitial || bInitial)
                return a[0] == b[0] ? 0.9 : 0.0;
            return Fuzz.JaroWinkler(a, b);
        }

        private static double Round(double v) => Math.Round(v, 4);
    }
}
