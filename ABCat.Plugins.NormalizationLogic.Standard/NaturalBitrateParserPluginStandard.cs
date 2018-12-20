using System;
using System.Linq;
using ABCat.Shared.Plugins.Catalog.ParsingLogics;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    [SingletoneComponentInfo("2.2")]
    [UsedImplicitly]
    public class NaturalBitrateParserPluginStandard : INaturalBitrateParserPlugin
    {
        private static readonly string[] BitrateJunkStrings = { "~", "vbr", "cbr" };

        private static readonly string[] BitrateKbpsStrings =
            {"kbps", "kbts", "kbit / sec", "kbit / s", "kbs", "kbp / s", "kb / s", "кb / s", "кbps", "kbit", "кбит / сек", "кбит / с", "кб / с", "к / с", "кбит", "кбс", "kpbs"};

        public void Dispose()
        {
        }

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }


        public bool TryParseBitrate(string bitrateString, out int bitrate)
        {
            bitrate = 0;
            if (!bitrateString.IsNullOrEmpty())
            {
                bitrateString = bitrateString.ToLower().ReplaceAll(BitrateKbpsStrings, "kbps");

                string mainPart;
                var parts = bitrateString.Split(",", " и ", "/");
                if (parts.Length > 1)
                    mainPart = parts
                        .FirstOrDefault(item =>
                            item.ContainsAny(BitrateKbpsStrings));
                else
                    mainPart = bitrateString;

                if (!mainPart.IsNullOrEmpty())
                {
                    mainPart = mainPart.ReplaceAll(BitrateJunkStrings, " ").Trim();
                    parts = mainPart.Split("kbps");
                    mainPart = parts.First().Trim().Split(" ").Last();
                    parts = mainPart.Split('-');
                    if (parts.Length == 1)
                        return int.TryParse(mainPart, out bitrate);

                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                        {
                            bitrate = (min + max) / 2;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}