using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared.Plugins.Catalog.ParsingLogics;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    [SingletoneComponentInfo("2.2")]
    [UsedImplicitly]
    public class NaturalTimeSpanParserPluginStandard : INaturalTimeSpanParserPlugin
    {
        private static readonly string[] HoursStrings =
            {"часа", "часов", "час.", "час", "ч.", "ч", "hours", "hour", "h"};

        private static readonly string[] MinutesStrings =
            {"минута", "минуты", "минут", "мин.", "мин", "м.", "м", "minutes", "min.", "min", "m"};

        private static readonly string[] SecondsStrings =
            {"секунда", "секунды", "секунд", "сек.", "сек", "с.", "с", "seconds", "sec.", "sec"};

        private static readonly string[] JunkStrings =
            {"примерно", "около", "~", "&#8776;"};

        public void Dispose()
        {
        }

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        public TimeSpan Parse(string timeSpanString)
        {
            var result = TimeSpan.Zero;

            var parts = timeSpanString.Split("+", " и ");
            if (parts.Length > 1)
                foreach (var part in parts)
                    result += SingleValueToTimeSpan(part);
            else
                result = SingleValueToTimeSpan(timeSpanString);

            return result;
        }

        private static TimeSpan SingleValueToTimeSpan(string timeSpanString)
        {
            var result = TimeSpan.Zero;

            if (!timeSpanString.IsNullOrEmpty())
            {
                List<string> lengths;
                timeSpanString = timeSpanString.ReplaceAll(JunkStrings, "").Trim();

                if (timeSpanString.Length >= 7 && TimeSpan.TryParse(timeSpanString, out result))
                {
                    lengths = new List<string>(timeSpanString.Split(":"));
                }
                else if (!TryToParseDotSpaceFormat(timeSpanString, out lengths))
                {
                    lengths = TryToResolveByKeywords(timeSpanString);
                }

                if (lengths.Count == 2) lengths.Insert(0, "00");

                if (lengths.Count >= 3)
                {
                    if (!short.TryParse(lengths[0], out var hour))
                    {
                        hour = 0;
                    }

                    if (!short.TryParse(lengths[1], out var minute))
                    {
                        minute = 0;
                    }

                    if (!short.TryParse(lengths[2], out var second))
                    {
                        second = 0;
                    }

                    result = new TimeSpan(hour, minute, second);
                }
            }

            return result;
        }

        private static bool TryToParseDotSpaceFormat(string length, out List<string> result)
        {
            result = length.Split(". ").ToList();
            if (result.Count > 1 && result.Count <= 3 && result.All(item => byte.TryParse(item, out var s1)))
            {
                if (result.Count == 2)
                {
                    result.Insert(0, "00");
                }

                return true;
            }

            return false;
        }

        private static List<string> TryToResolveByKeywords(string length)
        {
            List<string> lengths;
            var length4Parse = length.ToLower().ReplaceAll(new[] {" ", ",", "."}, "");

            var containsHours = length4Parse.ContainsAny(HoursStrings);
            var containsMinutes = length4Parse.ContainsAny(MinutesStrings);
            var containsSeconds = length4Parse.ContainsAny(SecondsStrings);

            if (containsHours || containsMinutes || containsSeconds)
            {
                length4Parse = length4Parse.ReplaceAll(HoursStrings, ":");
                length4Parse = length4Parse.ReplaceAll(MinutesStrings, ":");
                length4Parse = length4Parse.ReplaceAll(SecondsStrings, ":");

                lengths = new List<string>(length4Parse.Split(":"));

                if (lengths.Count >= 2 && containsHours && containsMinutes && !containsSeconds)
                {
                    lengths.Insert(2, "00");
                }
                else if (lengths.Count >= 1 && !containsHours && containsMinutes && !containsSeconds)
                {
                    lengths.Insert(0, "00");
                    lengths.Insert(2, "00");
                }
                else if (lengths.Count >= 1 && containsHours && !containsMinutes && !containsSeconds)
                {
                    lengths.Insert(1, "00");
                    lengths.Insert(2, "00");
                }
                else if (!containsHours && containsMinutes && containsSeconds)
                {
                    lengths.Insert(0, "00");
                }
                else if (lengths.Count >= 2 && !containsHours && containsMinutes)
                {
                    lengths.Insert(0, "00");
                    lengths.Insert(2, "00");
                }
            }
            else
            {
                if (length.Length <= 2)
                    lengths = new List<string> {"00", "00", length};
                else
                    lengths = new List<string>(length4Parse.Split(":"));
            }

            return lengths;
        }
    }
}