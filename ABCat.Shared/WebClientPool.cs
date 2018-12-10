using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace ABCat.Shared
{
    public static class WebClientPool
    {
        private static readonly ConcurrentBag<WebClient> WebClients = new ConcurrentBag<WebClient>();

        public static WebClientPoolItem GetPoolItem()
        {
            var result = new WebClientPoolItem {Target = {Encoding = Encoding.GetEncoding(1251)}};
            return result;
        }

        public static WebClient GetWebClient()
        {
            if (WebClients.TryTake(out var item)) return item;
            return new WebClient();
        }

        public static void PutObject(WebClient item)
        {
            WebClients.Add(item);
        }
    }
}