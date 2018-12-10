using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    public abstract class RecordTargetDownloaderBase : IRecordTargetDownloaderPlugin
    {
        /// <summary>
        ///     Сохранённые куки авторизации пользователя
        /// </summary>
        protected string LoginCookies;

        public Config Config { get; set; }

        /// <summary>
        ///     Адрес для авторизации
        /// </summary>
        protected abstract string LoginUrl { get; }

        public void BeginDownloadRecordTargetAsync(HashSet<string> recordsIds,
            Action<int, int, string> smallProgressCallback, Action<int, int, string> totalProgressCallback,
            Action<Exception> completedCallback, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var z = 0;
                    using (var dbContainer = Context.I.CreateDbContainer(true))
                    {
                        var records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsIds).ToArray();

                        foreach (var record in records)
                        {
                            totalProgressCallback(z, records.Count(), "{0} из {1}".F(z, records.Count()));
                            DownloadRecordTarget(null, record, dbContainer, smallProgressCallback,
                                cancellationToken);
                            if (cancellationToken.IsCancellationRequested) break;
                            dbContainer.SaveChanges();
                            z++;
                        }
                    }

                    completedCallback(null);
                }
                catch (Exception ex)
                {
                    completedCallback(ex);
                }
            }, cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfig);

        public void Dispose()
        {
            Disposed.Fire(this);
        }

        public event EventHandler Disposed;

        public abstract void DownloadRecordTarget(string loginCoockies, IAudioBook record, IDbContainer dbContainer,
            Action<int, int, string> progressCallback, CancellationToken cancellationToken);

        public abstract string GetAbsoluteLibraryPath(IAudioBook record);

        public abstract string GetRecordPageUrl(IAudioBook record);

        public abstract string GetRecordTargetLibraryPath(IAudioBook record);

        /// <summary>
        ///     Загрузка данных без кук
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected byte[] DownloadData(string url)
        {
            return DownloadData(url, string.Empty);
        }

        /// <summary>
        ///     Загрузка данных с куками
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookies"></param>
        /// <returns></returns>
        protected byte[] DownloadData(string url, string cookies)
        {
            using (var webClient = new WebClient())
            {
                PrepareWebClient4LoadTorrentFile(webClient);

                if (!string.IsNullOrEmpty(cookies))
                {
                    webClient.Headers.Add("Cookie", cookies);
                }

                return webClient.DownloadData(url);
            }
        }

        /// <summary>
        ///     Авторизация на сайте
        /// </summary>
        /// <returns></returns>
        protected virtual string GetLoginCookies(LoginInfo loginInfo)
        {
            if (string.IsNullOrEmpty(LoginCookies))
            {
                using (var webClient = new WebClient())
                {
                    PrepareWebClient4LoadTorrentFile(webClient);
                    //Логинимся
                    webClient.UploadValues(LoginUrl, "POST", GetLoginData(loginInfo));
                    //Берём куки
                    LoginCookies = RemoveSetCookiesAttributes(webClient.ResponseHeaders["Set-Cookie"]);
                }
            }

            return LoginCookies;
        }

        /// <summary>
        ///     Данные авторизации
        /// </summary>
        protected abstract NameValueCollection GetLoginData(LoginInfo loginInfo);

        /// <summary>
        ///     Преобразование респонз кук в реквест куки
        /// </summary>
        /// <param name="setCookieString"></param>
        /// <returns></returns>
        protected string RemoveSetCookiesAttributes(string setCookieString)
        {
            setCookieString += ",";
            setCookieString = Regex.Replace(setCookieString, "expires=[^;]*;", "");
            setCookieString = Regex.Replace(setCookieString, "path=[^;]*;", "");
            setCookieString = Regex.Replace(setCookieString, "domain=[^,]*,", "");
            return setCookieString;
        }

        /// <summary>
        ///     Веб клиент
        /// </summary>
        /// <returns></returns>
        private void PrepareWebClient4LoadTorrentFile(WebClient webClient)
        {
            webClient.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows; U; Windows NT 5.1; ru; rv:1.9.2.3) Gecko/20100401 Firefox/3.6.3 (.NET CLR 3.5.30729)");
            webClient.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            webClient.Headers.Add("Accept-Language", "ru,en-us;q=0.7,en;q=0.3");
            webClient.Headers.Add("Accept-Charset", "windows-1251,utf-8;q=0.7,*;q=0.7");
            webClient.Headers.Add("Keep-Alive", "115");
        }
    }
}