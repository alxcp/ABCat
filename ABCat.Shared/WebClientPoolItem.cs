using System;
using System.Net;
using System.Text;

namespace ABCat.Shared
{
    public sealed class WebClientPoolItem : IDisposable
    {
        public WebClientPoolItem()
        {
            Target = new WebClient(); // WebClientPool.GetWebClient();
            Target.Headers.Add("user-agent",
                "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");
        }

        public WebClient Target { get; private set; }

        /// <summary>
        ///     Download a page and decode it with the client's configured <see cref="WebClient.Encoding" />
        ///     (Windows-1251 for these sites). Uses raw bytes rather than
        ///     <see cref="WebClient.DownloadString(string)" />, whose response-charset heuristic returns
        ///     ISO-8859-1 after an http→https redirect and corrupts the Cyrillic text.
        /// </summary>
        public string DownloadString(string url)
        {
            return Decode(Target.DownloadData(url));
        }

        public string DownloadString(Uri url)
        {
            return Decode(Target.DownloadData(url));
        }

        private string Decode(byte[] data)
        {
            var encoding = Target.Encoding ?? Encoding.GetEncoding(1251);
            return encoding.GetString(data);
        }

        public void Dispose()
        {
            WebClientPool.PutObject(Target);
            Target = null;
        }
    }
}