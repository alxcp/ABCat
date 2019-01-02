using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [SingletoneComponentInfo("1.0")]
    [UsedImplicitly]
    public class WebSiteParserRutrackerAudioBooks : WebSiteParserBase
    {
        private const int RecordsOnPageCount = 50;

        private static readonly string[] GroupKeys =
        {
            "2388", "2387", "1036", "399", "400", "402", "490", "499", "574", "403", "716", "2165", "695",
            "661", "2348", "1279"
        };

        private readonly Stopwatch _lastLoadFromInternet = new Stopwatch();

        public override string DisplayName => "Rutracker";

        protected override string[] RecordPageJunkIdList { get; } =
        {
            "page_header", "page_footer", "ajax-loading", "ajax-error", "bb-alert-box", "modal-blocker", "pagination",
            "misc-hidden-elements", "pg-jump", "old-browser-warn", "invisible-heap", "preload"
        };

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

            var attachComment = document.GetNodesByClass("div", "attach_comment med").FirstOrDefault();
            attachComment?.ParentNode.RemoveChild(attachComment);
            var boldTCenter = document.GetNodesByClass("div", "bold tCenter mrg_8").FirstOrDefault();
            boldTCenter?.ParentNode.RemoveChild(boldTCenter);
            var charSet = document.DocumentNode.GetNodes("meta", "charset", "Windows-1251").FirstOrDefault();
            charSet?.SetAttributeValue("charset", "utf-8");

            foreach (var hideForPrintNode in document.DocumentNode.Descendants()
                .Where(item => item.HasClass("hide-for-print")).ToArray())
            {
                hideForPrintNode.ParentNode.RemoveChild(hideForPrintNode);
            }

            var textNodes = document.DocumentNode.Descendants("#text")
                .Where(item => item.InnerHtml.ReplaceAll(new[] {"\n", "\t"}, string.Empty).Trim() == string.Empty)
                .ToArray();

            foreach (var textNode in textNodes)
            {
                textNode.ParentNode.RemoveChild(textNode);
            }

            var commentNodes = document.DocumentNode.Descendants("#comment").ToArray();
            foreach (var commentNode in commentNodes)
            {
                commentNode.ParentNode.RemoveChild(commentNode);
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
                var pageData = pageSource == PageSources.WebOnly
                    ? null
                    : dbContainer.BinaryDataSet.GetByKey(recordPageKey);

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

        protected override void DownloadRecord(IDbContainer dbContainer, IAudioBook record, PageSources pageSource,
            CancellationToken cancellationToken)
        {
            string pageHtml = null;

            if (pageSource != PageSources.WebOnly)
            {
                //var pageMetaId = record.GetPageMetaKey();
                //var metaPage = dbContainer.BinaryDataSet.GetByKey(pageMetaId);
                //if (metaPage != null)
                //{
                //    pageHtml = metaPage.GetString();
                //}

                var page = dbContainer.BinaryDataSet.GetByKey(record.GetPageKey());

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

            if (pageHtml.IsNullOrEmpty() && pageSource != PageSources.CacheOnly)
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
                var progressMessage = audioBookGroup.Title +
                                      (pageCount > 0 ? $": {pageIndex} / {pageCount}" : string.Empty);

                ProgressMessage.Report(pageIndex, pageCount, progressMessage);

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

                if (sizeElement != null && sizeElement.ChildNodes.Count >= 2)
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

            var magnetA = document.DocumentNode.SelectSingleNode("//*[@class=\"magnet-link\"]");

            if (magnetA != null)
            {
                record.MagnetLink = magnetA.GetAttributeValue("href", null);
            }

            return page;
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