using System;
using System.Net;

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

        public void Dispose()
        {
            WebClientPool.PutObject(Target);
            Target = null;
        }
    }
}