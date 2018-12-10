using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.Parsers.Rutracker
{
    [SingletoneComponentInfo("1.0")]
    public class RutrackerTorrentFilesDownloader : RecordTargetDownloaderBase
    {
        private RutrackerTorrentFilesDownloaderConfig _config;

        protected override string LoginUrl => "http://login.rutracker.org/forum/login.php";

        public override void DownloadRecordTarget(string loginCoockies, IAudioBook record, IDbContainer dbContainer,
            Action<int, int, string> progressCallback, CancellationToken cancellationToken)
        {
            var config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();

            Directory.CreateDirectory(config.TorrentFilesFolder);

            var userData = dbContainer.UserDataSet.CreateUserData();
            userData.RecordGroupKey = record.GroupKey;
            userData.RecordKey = record.Key;

            var targetLibraryPath = GetAbsoluteLibraryPath(record);
            userData.LocalPath = targetLibraryPath;
            var commandLineArguments = "/directory \"{0}\" \"{1}\"".F(targetLibraryPath, record.MagnetLink);
            var ia = config.GetTorrentClient();
            Process.Start(ia.ExePath, commandLineArguments);

            dbContainer.UserDataSet.AddUserData(userData);
            dbContainer.SaveChanges();
        }

        public override string GetAbsoluteLibraryPath(IAudioBook record)
        {
            var config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();
            var bookPath = GetRecordTargetLibraryPath(record);
            return Path.Combine(config.AudioCatalogFolder, bookPath);
        }

        public override string GetRecordPageUrl(IAudioBook record)
        {
            return @"http://rutracker.org/forum/viewtopic.php?t={0}".F(record.Key);
        }

        public override string GetRecordTargetLibraryPath(IAudioBook record)
        {
            string result;
            if (record.Author != null)
            {
                result = GetFileName(record.Author);
            }
            else
            {
                result = GetFileName(record.Title);
            }

            return result;
        }

        /// <summary>
        ///     Пришлось перегрузить этот метод, поскольку рутрекер высылает куки
        ///     для авторизации одновременно с 302ым редиректом. WebClient
        ///     автоматически переходит на новый урл и теряет нужные нам куки.
        /// </summary>
        /// <returns></returns>
        protected override string GetLoginCookies([CanBeNull] LoginInfo loginInfo)
        {
            var config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();

            if (loginInfo == null && config.SaveCoockies && File.Exists(config.CoockieFileName))
            {
                LoginCookies = File.ReadAllText(config.CoockieFileName);
            }
            else if (loginInfo != null)
            {
                var sb = new StringBuilder();
                var loginData = GetLoginData(loginInfo);

                if (loginData != null)
                {
                    for (var i = 0; i < loginData.Count; i++)
                    {
                        sb.Append(string.Format("{0}={1}", loginData.AllKeys[i], loginData[i]));
                        if (i + 1 < loginData.Count) sb.Append('&');
                    }
                }

                var dataBytes = Encoding.UTF8.GetBytes(sb.ToString());

                var request = (HttpWebRequest) WebRequest.Create(LoginUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = dataBytes.Length;

                var requestStream = request.GetRequestStream();
                requestStream.Write(dataBytes, 0, dataBytes.Length);
                requestStream.Close();

                //ОЧЕНЬ ВАЖНО
                request.AllowAutoRedirect = false;

                var response = request.GetResponse();
                LoginCookies = RemoveSetCookiesAttributes(response.Headers["Set-Cookie"]);
            }

            if (!LoginCookies.IsNullOrEmpty() && LoginCookies.Length == 1) LoginCookies = null;

            if (!LoginCookies.IsNullOrEmpty())
            {
                var mainPage = DownloadData("http://rutracker.org/forum/index.php", LoginCookies);
                var pageText = Context.I.DefaultEncoding.GetString(mainPage);
                if (pageText.Contains("<form method=\"post\" action=\"http://login.rutracker.org/forum/login.php\">"))
                    LoginCookies = null;
                else
                {
                    if (config.SaveCoockies) File.WriteAllText(config.CoockieFileName, LoginCookies);
                }
            }

            return LoginCookies;
        }

        protected override NameValueCollection GetLoginData(LoginInfo loginInfo)
        {
            return new NameValueCollection
            {
                {"login_username", loginInfo.Login},
                {"login_password", loginInfo.GetPassword()},
                {"login", "Вход"}
            };
        }

        protected string GetTorrentFileUrl(IAudioBook record)
        {
            return @"http://dl.rutracker.org/forum/dl.php?t={0}".F(record.Key);
        }

        private string GetFileName(string rawFileName)
        {
            var sb = new StringBuilder();

            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (var c in rawFileName)
            {
                if (invalidChars.Contains(c)) sb.Append("_");
                else sb.Append(c);
            }

            return sb.ToString();
        }

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            _config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();
            var checkResult = _config.Check(correct);
            incorrectConfig = checkResult ? null : _config;
            return checkResult;
        }
    }
}