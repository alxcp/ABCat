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

namespace ABCat.Plugins.Parsers.Audioboo
{
    [UsedImplicitly]
    [SingletoneComponentInfo("2.2")]
    public class WebSiteParserAudiobooAudioBooks : WebSiteParserBase
    {
        // ReSharper disable StringLiteralTypo
        private static readonly string[] GroupKeys =
        {
            "antichnost", "illustr", "audiospektakl", "biz", "biogr", "boevik", "voina", "metrovsel", "didiktiva",
            "detsklit", "drama", "interviu", "istoria", "klassika", "lekcia", "lffr", "medicina", "mistic", "novella",
            "povest", "poznaem", "postapakalipsis", "poezia", "pritch", "prikluchenia", "proza", "psihologia",
            "publicictika", "rellign", "roman", "skazka", "ssstihi", "triller", "tresh", "ugas", "uchebnik",
            "fantastika", "filosophi", "fenezi", "horror", "ezoterika", "entogenez", "umor", "litrpg", "stalker",
            "warhammer-40000",
        };
        // ReSharper restore StringLiteralTypo

        private readonly Stopwatch _lastLoadFromInternet = new Stopwatch();

        public override string DisplayName => "Audioboo";

        public override Uri GetRecordPageUrl(IAudioBook record)
        {
            return new Uri($"http://audioboo.ru/{record.GroupKey}/{record.Key}");
        }

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        protected override string[] RecordPageJunkIdList { get; } = {"loading-layer"};

        public override IEnumerable<IAudioBookGroup> GetAllRecordGroups(IDbContainer dbContainer)
        {
            var result = GroupKeys.Select(groupKey => GetRecordGroup(groupKey, dbContainer));
            dbContainer.SaveChanges();
            return result;
        }

        protected override void CleanupRecordPage(HtmlDocument document)
        {
            base.CleanupRecordPage(document);
            var pageHeader = document.DocumentNode.Descendants("div").FirstOrDefault(item=>item.HasClass("header"));
            pageHeader?.ParentNode.RemoveChild(pageHeader);
            var noIndex = document.DocumentNode.Descendants("noindex").FirstOrDefault();
            noIndex?.ParentNode.RemoveChild(noIndex);

            var main = document.DocumentNode.Descendants("div").FirstOrDefault(item => item.HasClass("main"));
            var mainElements = main?.ChildNodes.ToArray();

            if (mainElements != null)
            {
                for (var z = 0; z < mainElements.Length - 1; z++)
                {
                    mainElements[z].ParentNode.RemoveChild(mainElements[z]);
                }
            }

            var fullNewsContentNode = document.DocumentNode.Descendants("div")
                .FirstOrDefault(item => item.HasClass("full-news-content"));

            var fullNewsContentElements = fullNewsContentNode?.ChildNodes.ToArray();
            if (fullNewsContentElements != null)
            {
                bool startDeletion = false;
                foreach (var fullNewsContentElement in fullNewsContentElements)
                {
                    if (startDeletion)
                        fullNewsContentElement.ParentNode.RemoveChild(fullNewsContentElement);
                    else if (fullNewsContentElement.GetAttributeValue("id", "").StartsWith("news-id-"))
                        startDeletion = true;
                }
            }

            var rightCol = document.DocumentNode.Descendants("div").FirstOrDefault(item => item.HasClass("right-col"));
            rightCol?.ParentNode.RemoveChild(rightCol);
            var footer = document.DocumentNode.Descendants("div").FirstOrDefault(item => item.HasClass("footer"));
            footer?.ParentNode.RemoveChild(footer);

            var charSet = document.DocumentNode.GetNodes("meta", "content", item=>item.Contains("charset=")).FirstOrDefault();
            charSet?.SetAttributeValue("content", charSet.GetAttributeValue("content", "").Replace("windows-1251", "utf-8"));

            var textNodes = document.DocumentNode.Descendants("#text").Where(item=>item.InnerHtml.Replace("\n","").Trim() == string.Empty).ToArray();
            foreach (var textNode in textNodes)
            {
                textNode.ParentNode.RemoveChild(textNode);
            }
        }

        [CanBeNull]
        protected override string GetRecordSourcePageString([NotNull] IAudioBook audioBook, PageSources pageSource,
            CancellationToken cancellationToken)
        {
            string result = null;

            using (var autoSave = Context.I.DbContainerAutoSave)
            {
                var dbContainer = autoSave.DBContainer;
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

        protected override void DownloadRecordGroup(IDbContainer dbContainer, IAudioBookGroup audioBookGroup, CancellationToken cancellationToken)
        {
            dbContainer.SaveChanges();

            var url = $"http://audioboo.ru/{audioBookGroup.Key}/";

            var pageIndex = 0;
            var pageCount = 0;

            do
            {
                string progressMessage = audioBookGroup.Title +
                                         (pageCount > 0 ? $": {pageIndex} / {pageCount}" : string.Empty);

                ProgressMessage.Report(pageIndex, pageCount, progressMessage);

                string pageHtml;

                using (var webClient = WebClientPool.GetPoolItem())
                {
                    var pageUrl = $"{url}page/{pageIndex + 1}/";
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
            var postBody = document.DocumentNode.Descendants("div")
                .FirstOrDefault(item => item.GetAttributeValue("id", "").StartsWith("news-id-"));

            if (postBody != null)
            {
                var elementsByRows = ParsePostBodyElementsByRows(postBody.InnerHtml);

                if (elementsByRows.TryGetValue("Общий размер", out var sizeText))
                {
                    var size = GetSizeInBytes(sizeText);
                    record.Size = size;
                }

                foreach (var element in elementsByRows)
                {
                    FillRecordElement(record, element.Key.TrimEnd(':'), element.Value);
                }
            }
        }

        private static int GetRecordGroupPageCount(HtmlDocument document)
        {
            var pagination = document.DocumentNode.Descendants("div")
                .FirstOrDefault(item => item.HasClass("navigation"));
            var lastPageNumber = 1;

            if (pagination != null)
            {
                lastPageNumber = pagination.Descendants("a")
                    .Max(item => int.TryParse(item.InnerText, out var result) ? result : 1);
            }

            return lastPageNumber;
        }

        private static string GetRecordGroupTitle(HtmlDocument document)
        {
            var head = document.DocumentNode.GetNodesByClass("head", null).FirstOrDefault();
            if (head == null) throw new Exception("Cannot find element 'head'");
            var title = head.GetNodesByClass("title", null).FirstOrDefault();
            if (title == null) throw new Exception("Cannot find element 'title'");
            var result = title.InnerText.Split(" &raquo; ").First().Trim();
            return result;
        }

        private static IEnumerable<HtmlNode> GetTopics(HtmlDocument document)
        {
            var mainContentWrap = document.GetElementbyId("dle-content");
            return mainContentWrap.GetNodesByClass("div", "biography-main");
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

            CleanupRecordPage(document);
            page = document.DocumentNode.InnerHtml;

            var binaryData = dbContainer.BinaryDataSet.CreateBinaryData();
            binaryData.Key = record.GetPageKey();
            binaryData.SetString(page, true);
            dbContainer.BinaryDataSet.AddChangedBinaryData(binaryData);

            record.MagnetLink = GetMagnet(document);

            return page;
        }

        private string GetMagnet(HtmlDocument document)
        {
            var table = document.DocumentNode.Descendants("table").FirstOrDefault(item => item.HasClass("btTbl"));
            var magnetRow = table?.Descendants("a")
                .FirstOrDefault(item => item.GetAttributeValue("href", "").StartsWith("magnet"));
            return magnetRow?.GetAttributeValue("href", "");
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

        private Dictionary<string, string> ParsePostBodyElementsByRows([NotNull] string postBodyHtml)
        {
            var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            var rows = postBodyHtml
                .Split(new[] {"<br>", "<tr class='row1'>"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.ReplaceAll(new[] {"&nbsp;", "\n"}, "")).ToArray();

            //"\n  <td class='genmed'>Общий размер:</td>\n  <td class='genmed'><font color='#123456'>409,93 Мб b</font></td>\n </tr>\n "

            if (rows.Length > 1)
            {
                // Skip(1) - skip title element
                foreach (var row in rows.Skip(1))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(row);
                    var str = doc.DocumentNode.InnerText.Trim();

                    if (!str.IsNullOrEmpty() && ParseKeyValue(str, out var key, out var value))
                    {
                        result[key] = value.Trim();
                    }
                }
            }

            return result;
        }

        private void ParseRecordGroupPage(IDbContainer dbContainer, IAudioBookGroup recordGroup, HtmlDocument document,
            CancellationToken cancellationToken)
        {
            var topics = new Dictionary<string, string>();

            foreach (var htmlNode in GetTopics(document))
            {
                var topicIdNode = htmlNode.GetNodesByClass("div", "biography-title").First().Descendants("a").First();
                var topicLink = topicIdNode.GetAttributeValue("href", string.Empty);
                var topicTitle = topicIdNode.InnerText;

                var topicId = topicLink.Split('/').Last();

                if (dbContainer.RecordsCache.Contains(topicId)) continue;
                dbContainer.RecordsCache.Add(topicId);
                topics.Add(topicId, topicTitle);
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

                    if (cancellationToken.IsCancellationRequested) return;

                    audioBook.GroupKey = recordGroup.Key;
                    audioBook.Title = topic.Value;
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