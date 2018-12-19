using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Monads;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace ABCat.Shared
{
    public static class Extensions
    {
        public enum CaseTypes
        {
            FirstWord,
            AllWords,
            AfterSplitter
        }

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";

        private static readonly Random Rnd = new Random();

        public static bool AnySafe<T>(this IEnumerable<T> value, Func<T, bool> predicate = null)
        {
            return value != null && (predicate == null ? value.Any() : value.Any(predicate));
        }

        public static string ChangeCase(this string text, CaseTypes caseType, bool changeOnlyTargetChar, bool trim)
        {
            var sb =
                new StringBuilder(changeOnlyTargetChar
                    ? (trim ? text.Trim(',', '.', ' ', '»', '«', '\"') : text)
                    : (trim ? text.ToLower().Trim(',', '.', ' ', '»', '«', '\"') : text.ToLower()));

            if (sb.Length > 0)
            {
                if (trim)
                {
                    int l;

                    do
                    {
                        l = sb.Length;
                        sb.Replace("  ", " ");
                    } while (l > sb.Length);

                    sb.Replace("|", "/");
                    sb.Replace("\\", "/");
                    sb.Replace("[", "(");
                    sb.Replace("]", ")");

                    InsertSpace(sb, ",", false, true);
                    InsertSpace(sb, "/", true, true);
                    InsertSpace(sb, ".", false, true);
                    InsertSpace(sb, "(", true, false);
                    InsertSpace(sb, ")", false, true);

                    if (sb[0] == ' ') sb.Remove(0, 1);
                    if (sb[sb.Length - 1] == ' ') sb.Remove(sb.Length - 1, 1);
                }

                sb[0] = sb[0].ToString(CultureInfo.InvariantCulture).ToUpper()[0];

                switch (caseType)
                {
                    case CaseTypes.AllWords:

                        for (var z = 1; z < sb.Length; z++)
                        {
                            if (sb[z] != ' ' && sb[z - 1] == ' ')
                            {
                                sb[z] = sb[z].ToString(CultureInfo.InvariantCulture).ToUpper()[0];
                            }
                        }

                        break;
                    case CaseTypes.AfterSplitter:
                        var splitter = false;

                        for (var z = 1; z < sb.Length; z++)
                        {
                            if (sb[z] == '.' || sb[z] == ',' || sb[z] == '/' || sb[z] == '\\' || sb[z] == '*' ||
                                sb[z] == '|')
                            {
                                splitter = true;
                            }
                            else if (sb[z] != ' ' && splitter)
                            {
                                sb[z] = sb[z].ToString(CultureInfo.InvariantCulture).ToUpper()[0];
                                splitter = false;
                            }
                        }

                        break;
                }
            }

            return sb.ToString();
        }

        public static void CheckAccess(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action, null);
        }

        public static string CollectExceptionDetails(this Exception ex)
        {
            string result = null;
            result += "Message: {0}\r\nStack: {1}\r\n---------\r\n".F(ex.Message, ex.StackTrace);
            if (ex.InnerException != null) result += ex.InnerException.CollectExceptionDetails();
            return result;
        }

        public static bool CompareEnd<T>(this List<T> collection, List<T> search)
        {
            var result = false;

            if (collection.Count >= search.Count)
            {
                result = true;
                for (var z = 0; z < search.Count; z++)
                {
                    if (!Equals(collection[collection.Count - z - 1], search[search.Count - z - 1]))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        public static IEnumerable<HtmlNode> EnumerateAllNodes(this HtmlNode rootNode)
        {
            var nodes = new Queue<HtmlNode>();

            nodes.Enqueue(rootNode);

            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                yield return node;
                foreach (var childNode in node.ChildNodes)
                {
                    nodes.Enqueue(childNode);
                }
            }
        }

        [StringFormatMethod("formatString")]
        public static string F(this string formatString, params object[] args)
        {
            return string.Format(formatString, args);
        }

        public static void Fire(this EventHandler eventHandler, object sender, EventArgs e = null)
        {
            eventHandler?.Invoke(sender, e ?? EventArgs.Empty);
        }

        public static void Fire<T>(this EventHandler<T> eventHandler, object sender, T e)
            where T : EventArgs
        {
            eventHandler?.Invoke(sender, e);
        }

        public static string GenerateRandomString(int size, int? seed = null)
        {
            var buffer = new char[size];

            var rnd = seed.HasValue ? new Random(seed.Value) : Rnd;

            for (var i = 0; i < size; i++)
            {
                buffer[i] = Chars[rnd.Next(Chars.Length)];
            }

            return new string(buffer);
        }

        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static object GetEnumValueFromDescription(this string description, Type enumType)
        {
            if (!enumType.IsEnum) throw new InvalidOperationException();
            foreach (var field in enumType.GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return field.GetValue(null);
                }
            }

            throw new ArgumentException("Not found.", "description");
            // or return default(T);
        }

        [UsedImplicitly]
        public static IEnumerable<InstalledApplication> GetInstalledApplications()
        {
            var result = new List<InstalledApplication>();
            Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
                .Do(item => result.AddRange(GetInstalledApplications(item)));
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
                .Do(item => result.AddRange(GetInstalledApplications(item)));
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
                .Do(item => result.AddRange(GetInstalledApplications(item)));
            return result;
        }

        public static IEnumerable<HtmlNode> GetNodes(this HtmlNode node, string nodeName, string attributeName,
            string attributeValue)
        {
            return node.Descendants(nodeName)
                .Where(item => item.GetAttributeValue(attributeName, null) == attributeValue);
        }

        public static IEnumerable<HtmlNode> GetNodes(this HtmlNode node, string nodeName, string attributeName,
            Func<string, bool> predicate)
        {
            return node.Descendants(nodeName).Where(item => predicate(item.GetAttributeValue(attributeName, null)));
        }

        public static IEnumerable<HtmlNode> GetNodes(this HtmlDocument document, string nodeName, string attributeName,
            string attributeValue)
        {
            return document.DocumentNode.GetNodes(nodeName, attributeName, attributeValue);
        }

        public static IEnumerable<HtmlNode> GetNodes(this HtmlDocument document, string nodeName, string attributeName,
            Func<string, bool> predicate)
        {
            return document.DocumentNode.GetNodes(nodeName, attributeName, predicate);
        }

        public static IEnumerable<HtmlNode> GetNodesByClass(this HtmlNode node, string nodeName, string attributeValue)
        {
            return node.GetNodes(nodeName, "class", attributeValue);
        }

        public static IEnumerable<HtmlNode> GetNodesByClass(this HtmlDocument document, string nodeName,
            string attributeValue)
        {
            return document.GetNodes(nodeName, "class", attributeValue);
        }

        public static void InsertSpace(StringBuilder sb, string sign, bool spaceBefore, bool spaceAfter)
        {
            sb.Replace("{0} ".F(sign), sign);
            sb.Replace(" {0}".F(sign), sign);

            if (spaceBefore) sb.Replace(sign, " {0}".F(sign));
            if (spaceAfter) sb.Replace(sign, "{0} ".F(sign));
        }

        [ContractAnnotation("value:null => true")]
        public static bool IsNullOrEmpty([CanBeNull] this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static void Pack(IEnumerable<string> sourceFiles, string topDirectory, string packFile)
        {
            if (File.Exists(packFile))
            {
                File.Delete(packFile);
            }

            using (var fs = new FileStream(packFile, FileMode.Create, FileAccess.Write))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write("1.0");
                    bw.Write("1.0");
                    bw.Write(sourceFiles.Count());

                    foreach (var sourceFile in sourceFiles)
                    {
                        var sourceContent = File.ReadAllBytes(sourceFile);
                        bw.Write(sourceFile.Replace(topDirectory, ""));
                        bw.Write(sourceContent.Length);
                        bw.Write(sourceContent);
                    }
                }
            }
        }

        public static int Randomize(this int value, int range)
        {
            return value - range + Rnd.Next(range * 2);
        }

        public static string[] Split(this string value, params string[] separators)
        {
            return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        public static byte[] ToBitmap(this FrameworkElement element)
        {
            var targetBitmap =
                new RenderTargetBitmap((int) element.ActualWidth,
                    (int) element.ActualHeight,
                    96d, 96d,
                    PixelFormats.Default);
            targetBitmap.Render(element);

            // add the RenderTargetBitmap to a Bitmapencoder
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(targetBitmap));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public static string ToStringOrEmpty(this object value)
        {
            return value == null ? string.Empty : value.ToString();
        }

        public static TimeSpan ToTimeSpan(this string lengthString)
        {
            TimeSpan result = TimeSpan.Zero;

            var parts = lengthString.Split("+", " и ");
            if (parts.Length > 1)
            {
                foreach (var part in parts)
                {
                    result += SingleValueToTimeSpan(part);
                }
            }
            else
            {
                result = SingleValueToTimeSpan(lengthString);
            }

            return result;
        }

        private static TimeSpan SingleValueToTimeSpan(string lengthString)
        {
            var result = TimeSpan.Zero;

            if (!IsNullOrEmpty(lengthString))
            {
                List<string> lengths;
                lengthString = ReplaceExtraWords(lengthString);

                if (lengthString.Length >= 7 && TimeSpan.TryParse(lengthString, out result))
                {
                    lengths = new List<string>(lengthString.Split(":"));
                }
                else if (!TryToParseDotSpaceFormat(lengthString, out lengths))
                {
                    lengths = TryToResolveByKeywords(lengthString);
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

        private static string ReplaceExtraWords(string length)
        {
            return length.ToLower()
                .Replace("примерно", "")
                .Replace("около", "")
                .Replace("~", "")
                .Replace("&#8776;", "")
                .Trim();
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
            var length4Parse =
                length.ToLower()
                    .Replace(" ", "")
                    .Replace(",", "")
                    .Replace(".", "");

            var hours = new[] {"часа", "часов", "час.", "час", "ч.", "ч", "hours", "hour", "h"};
            var minutes = new[]
                {"минута", "минуты", "минут", "мин.", "мин", "м.", "м", "minutes", "min.", "min", "m"};
            var seconds = new[] {"секунда", "секунды", "секунд", "сек.", "сек", "с.", "с", "seconds", "sec.", "sec"};

            foreach (var hour in hours)
            {
                var length1 = length4Parse.Replace(hour, "*-*");
                length4Parse = length1;
            }

            foreach (var minute in minutes)
            {
                var length1 = length4Parse.Replace(minute, "*--*");
                length4Parse = length1;
            }

            foreach (var second in seconds)
            {
                var length1 = length4Parse.Replace(second, "*---*");
                length4Parse = length1;
            }

            var containsHours = length4Parse.Contains("*-*");
            var containsMinutes = length4Parse.Contains("*--*");
            var containsSeconds = length4Parse.Contains("*---*");

            if (containsHours || containsMinutes || containsSeconds)
            {
                length4Parse = length4Parse.Replace("*-*", ":");
                length4Parse = length4Parse.Replace("*--*", ":");
                length4Parse = length4Parse.Replace("*---*", ":");

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

        public static void UnPack(string packFile, string targetPath, Action<int> reportProgressCallBack)
        {
            using (var fs = new FileStream(packFile, FileMode.Open, FileAccess.Read))
            {
                using (var br = new BinaryReader(fs))
                {
                    var version = br.ReadString();
                    var softVersion = br.ReadString();

                    if (version == "1.0")
                    {
                        try
                        {
                            var filesCount = br.ReadInt32();

                            for (var z = 0; z < filesCount; z++)
                            {
                                var filePathName = Path.Combine(targetPath, br.ReadString());

                                var length = br.ReadInt32();
                                try
                                {
                                    File.WriteAllBytes(filePathName, br.ReadBytes(length));
                                    reportProgressCallBack((int) (fs.Position / (double) fs.Length) * 100);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Ошибка создания файла '{0}'".F(filePathName), ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Ошибка чтения пакета '{0}'".F(packFile), ex);
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "Невозможно открыть пакет. Требуется версия программы не ниже '{0}'. Версия пакета: '{1}'."
                                .F(
                                    softVersion, version));
                    }
                }
            }
        }

        private static IEnumerable<InstalledApplication> GetInstalledApplications([NotNull] RegistryKey key)
        {
            var result = new List<InstalledApplication>();
            foreach (var keyName in key.GetSubKeyNames())
            {
                key.OpenSubKey(keyName).Do(subKey =>
                {
                    var displayName = subKey.GetValue("DisplayName").ToStringOrEmpty();
                    result.Add(new InstalledApplication
                    {
                        DisplayName = displayName,
                        DisplayVersion = subKey.GetValue("DisplayVersion").ToStringOrEmpty(),
                        InstallLocation = subKey.GetValue("InstallLocation").ToStringOrEmpty(),
                        UninstallString = subKey.GetValue("UninstallString").ToStringOrEmpty()
                    });
                });
            }

            return result;
        }

        public class InstalledApplication
        {
            public string DisplayName { get; set; }
            public string DisplayVersion { get; set; }
            public string ExePath { get; set; }
            public string InstallLocation { get; set; }
            public string UninstallString { get; set; }

            public bool CheckExists()
            {
                return !string.IsNullOrEmpty(InstallLocation) && File.Exists(InstallLocation);
            }
        }
    }
}