using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ABCat.Shared;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace ABCat.Plugins.Parsers.Rutracker
{
    [SingletoneComponentInfo("1.0", false)]
    [UsedImplicitly]
    public class SiteParserRutrackerAudioBooks : SiteParserBase
    {
        private const int RecordsOnPageCount = 50;

        private static readonly string[] GroupKeys = {
            "2388", "2387", "1036", "399", "400", "402", "490", "499", "574", "403", "716", "2165", "695",
            "661", "2348", "1279"
        };

        private readonly Stopwatch _lastLoadFromInternet = new Stopwatch();

        public override Uri GetRecordPageUrl(IAudioBook record)
        {
            return new Uri($"http://rutracker.org/forum/viewtopic.php?t={record.Key}");
        }

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        public override IEnumerable<IAudioBookGroup> GetAllRecordGroups(IDbContainer dbContainer)
        {
            var result = GroupKeys.Select(groupKey => GetRecordGroup(groupKey, dbContainer));
            dbContainer.SaveChanges();
            return result;
        }

        protected override void CleanupRecordPage(HtmlDocument document)
        {
            base.CleanupRecordPage(document);
            var pageHeader = document.GetElementbyId("page_header");
            pageHeader?.ParentNode.RemoveChild(pageHeader);
            var pageFooter = document.GetElementbyId("page_footer");
            pageFooter?.ParentNode.RemoveChild(pageFooter);
            var attachComment = document.GetNodesByClass("div", "attach_comment med").FirstOrDefault();
            attachComment?.ParentNode.RemoveChild(attachComment);
            var boldTCenter = document.GetNodesByClass("div", "bold tCenter mrg_8").FirstOrDefault();
            boldTCenter?.ParentNode.RemoveChild(boldTCenter);
            var charSet = document.DocumentNode.GetNodes("meta", "charset", "Windows-1251").FirstOrDefault();
            charSet?.SetAttributeValue("charset", "utf-8");

            var nodeNames = new[] {"td", "p", "tbody", "div"};

            foreach (var nodeName in nodeNames)
            {
                foreach (var hideForPrintNode in document.GetNodes(nodeName, "class", cls=>!cls.IsNullOrEmpty() && cls.Contains("hide-for-print")).ToList())
                {
                    hideForPrintNode.ParentNode.RemoveChild(hideForPrintNode);
                }
            }
        }

        [CanBeNull]
        protected override string GetRecordSourcePageString([NotNull] IAudioBook audioBook, PageSources pageSource,
            CancellationToken cancellationToken)
        {
            string result = null;

            using (var dbContainer = Context.I.CreateDbContainer(true))
            {
                var recordPageKey = audioBook.GetPageKey();
                var pageData = pageSource == PageSources.WebOnly ? null : dbContainer.BinaryDataSet.GetByKey(recordPageKey);

                if (pageData != null)
                {
                    var data = pageData.GetData();
                    using (var ms = new MemoryStream(data))
                    {
                        var document = new HtmlDocument();
                        document.Load(ms);
                        if (cancellationToken.IsCancellationRequested) return null;

                        CleanupRecordPage(document);
                        result = document.DocumentNode.InnerHtml;

                        if (cancellationToken.IsCancellationRequested) return null;
                        pageData = dbContainer.BinaryDataSet.CreateBinaryData();
                        pageData.Key = recordPageKey;
                        pageData.SetString(result, true);
                        dbContainer.BinaryDataSet.AddChangedBinaryData(pageData);
                    }
                }
                else if (pageSource != PageSources.CacheOnly)
                {
                    using (var webClient = WebClientPool.GetPoolItem())
                    {
                        var pageUrl = GetRecordPageUrl(audioBook);
                        result = webClient.Target.DownloadString(pageUrl);

                        var document = new HtmlDocument();
                        document.LoadHtml(result);
                        if (cancellationToken.IsCancellationRequested) return null;

                        CleanupRecordPage(document);
                        result = document.DocumentNode.InnerHtml;

                        if (cancellationToken.IsCancellationRequested) return null;
                        pageData = dbContainer.BinaryDataSet.CreateBinaryData();
                        pageData.Key = recordPageKey;
                        pageData.SetString(result, true);
                        dbContainer.BinaryDataSet.AddChangedBinaryData(pageData);
                    }
                }
            }

            return result;
        }

        protected override void DownloadRecord(IDbContainer dbContainer, IAudioBook record, PageSources pageSource, CancellationToken cancellationToken)
        {
            string pageHtml = null;

            if (pageSource != PageSources.WebOnly)
            {
                var pageMetaId = record.GetPageMetaKey();
                var metaPage = dbContainer.BinaryDataSet.GetByKey(pageMetaId);
                if (metaPage != null)
                {
                    pageHtml = metaPage.GetString();
                }

                IBinaryData page = dbContainer.BinaryDataSet.GetByKey(record.GetPageKey());

                if (page != null)
                {
                    using (var ms = new MemoryStream(page.GetData()))
                    {
                        var doc = new HtmlDocument();
                        doc.Load(ms);
                        var savedHtml = doc.DocumentNode.OuterHtml;
                        CleanupRecordPage(doc);
                        pageHtml = doc.DocumentNode.InnerHtml;

                        if (savedHtml != pageHtml)
                        {
                            page.SetString(doc.DocumentNode.InnerHtml, true);
                            dbContainer.BinaryDataSet.AddChangedBinaryData(page);
                        }
                    }
                }
            }

            if (pageHtml.IsNullOrEmpty())
            {
                pageHtml = DownloadRecordMetaPageFromWeb(record, dbContainer, cancellationToken);
            }

            if (pageHtml.IsNullOrEmpty() || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            ParseRecord(dbContainer, record, pageHtml);
        }

        protected override void DownloadRecordGroup(IDbContainer dbContainer, IAudioBookGroup audioBookGroup,
            CancellationToken cancellationToken)
        {
            dbContainer.SaveChanges();

            var url = $"http://rutracker.org/forum/viewforum.php?f={audioBookGroup.Key}";

            var pageIndex = 0;
            var pageCount = 0;

            do
            {
                ProgressMessage.Report(pageIndex, pageCount, audioBookGroup.Title);

                string pageHtml;

                using (var webClient = WebClientPool.GetPoolItem())
                {
                    var pageUrl = $"{url}&start={pageIndex * RecordsOnPageCount}";
                    pageHtml = webClient.Target.DownloadString(pageUrl);
                    if (cancellationToken.IsCancellationRequested) return;
                }

                var document = new HtmlDocument();
                document.LoadHtml(pageHtml);

                if (pageCount == 0)
                {
                    UpdateRecordGroupInfo(audioBookGroup, document);
                    dbContainer.AudioBookGroupSet.AddChangedRecordGroups(audioBookGroup);
                    pageCount = audioBookGroup.LastPageCount;
                }

                ParseRecordGroupPage(dbContainer, audioBookGroup, document, cancellationToken);

                Thread.Sleep(200);

                pageIndex++;
            } while (pageIndex < pageCount);

            if (cancellationToken.IsCancellationRequested) return;
            audioBookGroup.LastUpdate = DateTime.Now;
        }

        protected override void ParseRecord(IDbContainer dbContainer, IAudioBook record, string postHtml)
        {
            record.ClearMetaInfo();

            var document = new HtmlDocument();
            document.LoadHtml(postHtml);
            var topicMain = document.GetElementbyId("topic_main");
            var postBody = topicMain?.Descendants().FirstOrDefault(item => item.HasClass("post_body"));

            if (postBody != null)
            {
                var elementsByRows = ParsePostBodyElementsByRows(postBody.InnerHtml);

                var sizeElement = document.DocumentNode.Descendants()
                    .FirstOrDefault(item => item.HasClass("attach_link") && item.HasClass("guest"));

                if (sizeElement != null && sizeElement.ChildNodes.Count == 3)
                {
                    var sizeNode = sizeElement.LastChild;
                    var size = GetSizeInBytes(sizeNode.InnerText.ReplaceAll(new[] {"&middot;", "&nbsp;"}, " ")
                        .Trim(' ', '\t'));
                    record.Size = size;
                }

                foreach (var element in elementsByRows)
                {
                    FillRecordElement(record, element.Key.TrimEnd(':'), element.Value);
                }
            }
        }

        private static long GetSizeInBytes(string sizeString)
        {
            long result = 0;
            var subSize = sizeString.Split(' ');
            if (subSize.Length == 2 && float.TryParse(subSize[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out var size))
            {
                int multiplier = 0;

                switch (subSize[1])
                {
                    case "GB":
                        multiplier = 1024 * 1024 * 1024;
                        break;
                    case "MB":
                        multiplier = 1024 * 1024;
                        break;
                    case "KB":
                        multiplier = 1024;
                        break;
                    default:
                        var t = 0;
                        break;
                }

                result = (long) (size * multiplier);
            }

            return result;
        }

        private static int GetRecordGroupPageCount(HtmlDocument document)
        {
            var pagination = document.GetElementbyId("pagination");
            var lastPageNumber = 1;

            if (pagination != null)
            {
                var pg = pagination.GetNodesByClass("p", null).ToArray();

                if (pg.AnySafe())
                {
                    var p = pg.Skip(1).Take(1).First();
                    lastPageNumber = p.GetNodesByClass("a", "pg").Select(item =>
                    {
                        int.TryParse(item.InnerText, out var a);
                        return a;
                    }).Max();
                }
            }

            return lastPageNumber;
        }

        private static string GetRecordGroupTitle(HtmlDocument document)
        {
            var head = document.DocumentNode.GetNodesByClass("head", null).FirstOrDefault();
            if (head == null) throw new Exception("Cannot find element 'head'");
            var title = head.GetNodesByClass("title", null).FirstOrDefault();
            if (title == null) throw new Exception("Cannot find element 'title'");
            var result = title.InnerText.Split("[стр. 1]").First().Replace("[Аудио]", "").Trim();
            return result;
        }

        private static IEnumerable<HtmlNode> GetTopics(HtmlDocument document)
        {
            var mainContentWrap = document.GetElementbyId("main_content_wrap");
            return mainContentWrap.GetNodesByClass("tr", "hl-tr");
        }

        private static string GetTopicTitle(HtmlNode topicNode, string topicId)
        {
            var result = topicNode.GetNodes("a", "id", "tt-" + topicId).First().InnerText.Replace("&quot;", "\"");
            result = result.Split('[')[0];
            return result;
        }

        private string DownloadRecordMetaPageFromWeb(IAudioBook record, IDbContainer dbContainer,
            CancellationToken cancellationToken)
        {
            while (_lastLoadFromInternet.IsRunning && _lastLoadFromInternet.ElapsedMilliseconds < 1000)
            {
                Thread.Sleep(200);
                if (cancellationToken.IsCancellationRequested) return null;
            }

            string page;
            using (var webClient = WebClientPool.GetPoolItem())
            {
                var pageUrl = GetRecordPageUrl(record);
                page = webClient.Target.DownloadString(pageUrl);
                _lastLoadFromInternet.Restart();
                if (cancellationToken.IsCancellationRequested) return null;
            }

            var document = new HtmlDocument();
            document.LoadHtml(page);
            if (cancellationToken.IsCancellationRequested) return null;

            //ClearRecordPage(document);
            page = document.DocumentNode.InnerHtml;

            var pageKey = record.GetPageKey();

            var binaryData = dbContainer.BinaryDataSet.CreateBinaryData();
            binaryData.Key = pageKey;
            binaryData.SetString(page, true);
            dbContainer.BinaryDataSet.AddChangedBinaryData(binaryData);

            var result = GetPostBody(document);
            var magnetA = document.DocumentNode.SelectSingleNode("//*[@class=\"magnet-link\"]");

            if (magnetA != null)
            {
                record.MagnetLink = magnetA.GetAttributeValue("href", null);
            }

            if (result != null)
            {
                var pageMetaBinaryData = dbContainer.BinaryDataSet.CreateBinaryData();
                pageMetaBinaryData.Key = record.GetPageMetaKey();
                pageMetaBinaryData.SetString(result.InnerHtml, false);
                dbContainer.BinaryDataSet.AddChangedBinaryData(pageMetaBinaryData);
            }
            else
            {
                // ToDo: Скорее всего топик был удалён с сайта. Нужна проверка что это именно так - парсинг страницы удалённого топика и пометка записи как удалённой
                return null;
            }

            return result.InnerHtml;
        }

        private HtmlNode GetPostBody(HtmlDocument document)
        {
            var postBody = document.DocumentNode.GetNodesByClass("div", "post_body").First();

            return postBody;
        }

        //private static class KeysCollection
        //{
        //    private static Stopwatch _sw = new Stopwatch();
        //    private static readonly Dictionary<string, HashSet<string>> _values = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);

        //    public static void Add(string key, string value)
        //    {
        //        if (!_values.TryGetValue(key, out var values))
        //        {
        //            values = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        //            _values.Add(key, values);
        //        }

        //        values.Add(value);

        //        if (!_sw.IsRunning)
        //            _sw.Start();

        //        if (_sw.Elapsed.TotalSeconds > 15)
        //        {
        //            Debug.WriteLine("===============================");
        //            var top50 = _values.Where(item => item.Value.Count > 10);
        //            foreach (var keyValuePair in top50.OrderByDescending(item=>item.Value.Count))
        //            {
        //                Debug.WriteLine($"{keyValuePair.Key}\t{keyValuePair.Value.Count}");
        //            }

        //            Debug.WriteLine("===============================");
        //            _sw.Restart();
        //        }
        //    }
        //}

        private void FillRecordElement(IAudioBook record, string key, string value)
        {
            key = key.Trim(' ', '[', ']', '<', '>', ';', '\n', '\t', '.', ',', '"', '(');

//#if DEBUG
//            KeysCollection.Add(key, value);
//#endif

            switch (key.ToLower())
            {
                case "автор":
                case "авторы":
                case "фамилия, имя автора":
                case "фамилии авторов":
                case "авторы произведений":
                    record.Author = CleanupRecordValue(value, false, 500)
                        .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "фамилия автора":
                    record.AuthorSurnameForParse =
                        CleanupRecordValue(value, false, 250).ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "имя автора":
                    record.AuthorNameForParse =
                        CleanupRecordValue(value, false, 250).ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "издательство":
                    record.Publisher =
                        CleanupRecordValue(value, false, 500).ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "исполнитель":
                case "исполнители":
                case "запись и обработка":
                    record.Reader = CleanupRecordValue(value, false, 500)
                        .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "жанр":
                    record.Genre =
                        CleanupRecordValue(value, false, 200)
                            .ChangeCase(Extensions.CaseTypes.AfterSplitter, false, true);
                    break;
                case "битрейт":
                case "битрейт аудио":
                    record.Bitrate = CleanupRecordValue(value, false, 100)
                        .ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "длительность":
                case "прдолжительность":
                case "продолжительность":
                case "продолжительность звучания":
                case "продолжительность книги":
                case "общее время звучания":
                case "общее время воспроизведения":
                case "bремя звучания":
                case "время звучания":
                case "время воспроизведения":
                case "время чтения":
                case "общая продолжительность":
                case "продолжительность аудиокниги":
                case "продолжительность (время звучания)":
                case "время":
                case "длина записи":
                    record.Length = CleanupRecordValue(value, false, 500);
                    break;
                case "описание":
                case "аннотация":
                    record.Description = CleanupRecordValue(value, true, 1000);
                    break;
                case "доп. информация":
                    if (ParseKeyValue(value, out var secondKey, out var secondValue))
                    {
                        FillRecordElement(record, secondKey, secondValue);
                    }

                    break;
            }
        }

        private IAudioBookGroup GetRecordGroup(string recordGroupKey, IDbContainer dbContainer)
        {
            var result = dbContainer.AudioBookGroupSet.GetRecordGroupByKey(recordGroupKey);
            if (result == null)
            {
                result = dbContainer.AudioBookGroupSet.CreateRecordGroup();
                dbContainer.AudioBookGroupSet.AddRecordGroup(result);
                result.Key = recordGroupKey;
                result.Title = recordGroupKey;
            }

            return result;
        }

        private bool ParseKeyValue(string keyValue, out string key, out string value)
        {
            var result = false;

            key = null;
            value = null;

            if (!keyValue.IsNullOrEmpty())
            {
                var iofColon = keyValue.IndexOf(':');
                if (iofColon > 0 && iofColon < keyValue.Length - 1)
                {
                    key = keyValue.Substring(0, iofColon).ToLower();
                    var iofSemiColon = key.LastIndexOf(';');
                    if (iofSemiColon > 0)
                    {
                        key = key.Substring(iofSemiColon + 1, key.Length - iofSemiColon - 1);
                    }

                    key = key.TrimStart('-', '\n', '.').Trim(' ');

                    if (!Extensions.IsNullOrEmpty(key))
                    {
                        value = keyValue.Substring(iofColon + 1, keyValue.Length - iofColon - 1);
                        result = true;
                    }
                }
            }

            return result;
        }

        private Dictionary<string, string> ParsePostBodyElementsByRows([NotNull] string postBodyHtml)
        {
            var result = new Dictionary<string, string>();

            var rows = postBodyHtml
                .Split(new[] {"<br>", "<hr class=\"post-hr\">", "<span class=\"post-b\">"},
                    StringSplitOptions.RemoveEmptyEntries).Select(item => item.TrimStart('\n', '"'))
                .Where(item => !item.IsNullOrEmpty() && item != "</span>").ToArray();


            if (rows.Length > 1)
            {
                rows[1] = string.Join("", rows[1].Split("</var>").Skip(1));

                // Skip(1) - skip post title
                foreach (var row in rows.Skip(1))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(row);
                    var str = doc.DocumentNode.InnerText.TrimStart('\n', '"');

                    if (!str.IsNullOrEmpty() && ParseKeyValue(str, out var key, out var value))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }

        private void ParseRecordGroupPage(IDbContainer dbContainer, IAudioBookGroup recordGroup, HtmlDocument document,
            CancellationToken cancellationToken)
        {
            var topics = new Dictionary<string, HtmlNode>();

            foreach (var htmlNode in GetTopics(document))
            {
                var topicId = htmlNode.GetNodesByClass("td", "vf-col-icon vf-topic-icon-cell").First()
                    .GetAttributeValue("id", "");
                if (dbContainer.RecordsCache.Contains(topicId)) continue;
                dbContainer.RecordsCache.Add(topicId);
                topics.Add(topicId, htmlNode);
            }

            var records =
                dbContainer.AudioBookSet.GetRecordsByKeys(new HashSet<string>(topics.Keys))
                    .ToDictionary(record => record.Key, record => record);

            foreach (var topic in topics)
            {
                try
                {
                    if (!records.TryGetValue(topic.Key, out var audioBook))
                    {
                        audioBook = dbContainer.AudioBookSet.CreateRecord();
                        audioBook.Created = DateTime.Now;
                        audioBook.Key = topic.Key;
                        audioBook.Genre = string.Empty;
                        audioBook.Author = string.Empty;
                        records.Add(topic.Key, audioBook);
                        dbContainer.AudioBookSet.AddRecord(audioBook);
                    }

                    var topicTitle = GetTopicTitle(topic.Value, topic.Key);

                    if (cancellationToken.IsCancellationRequested) return;

                    audioBook.GroupKey = recordGroup.Key;
                    audioBook.Title = topicTitle;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void UpdateRecordGroupInfo(IAudioBookGroup recordGroup, HtmlDocument document)
        {
            recordGroup.Title = GetRecordGroupTitle(document);
            recordGroup.LastPageCount = GetRecordGroupPageCount(document);
        }
    }
}