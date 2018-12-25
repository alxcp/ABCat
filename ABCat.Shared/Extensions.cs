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
            result += $"Message: {ex.Message}\r\nStack: {ex.StackTrace}\r\n---------\r\n";
            if (ex.InnerException != null) result += ex.InnerException.CollectExceptionDetails();
            return result;
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
            sb.Replace(sign + " ", sign);
            sb.Replace(" " + sign, sign);

            if (spaceBefore) sb.Replace(sign, " " + sign);
            if (spaceAfter) sb.Replace(sign, sign + " ");
        }

        [ContractAnnotation("value:null => true")]
        public static bool IsNullOrEmpty([CanBeNull] this string value)
        {
            return string.IsNullOrEmpty(value);
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