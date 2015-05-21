using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Sogaard.Tools.Scraping.Multithreading;

namespace Sogaard.Tools.Scraping
{
    public static class ScraperHelper
    {
        public static void AddHeadersToClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Accept-Language", "da-DK,da;q=0.8,en-US;q=0.6,en;q=0.4");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36");
            client.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
        }
        public static void SetOrigenToClient(string url, HttpClient client)
        {
            var hostname = new Uri(url).Host;
            if (client.DefaultRequestHeaders.Contains("Host"))
            {
                client.DefaultRequestHeaders.Remove("Host");
            }
            client.DefaultRequestHeaders.Add("Host", hostname);
        }

        public static void AddConsoleLogging(ThreadedWebClientWorker downloader)
        {

            downloader.DownloaderThreadJobChanged += (sender, id, job) =>
            {
                if (job == null)
                {
                    //  Console.WriteLine("D[" + id + "] Inavtive");
                }
                else
                {
                    Console.WriteLine("D[" + id + "] " + job);
                }
            };
            downloader.DownloaderThreadStatus += (sender, id, status) =>
            {
                if (status)
                {
                    //Console.WriteLine("D[" + id + "] active");
                }
                else
                {
                    //  Console.WriteLine("D[" + id + "] Inavtive");
                }
            };
            downloader.WorkerThreadError += (sender, id, job, exp) =>
            {
                Console.WriteLine("W[" + id + "] Error:  " + job);
                Console.WriteLine(exp.Message);
            };
            downloader.WorkerThreadJobDone += (sender, id, job) =>
            {
                Console.WriteLine("W[" + id + "] Done:  " + job);
            };
            downloader.WorkerThreadStatusChanged += (sender, id, job) =>
            {
                if (job == null)
                {
                    // Console.WriteLine("W[" + id + "] Inavtive");
                }
                else
                {
                    Console.WriteLine("W[" + id + "] " + job);
                }
            };
            downloader.DownloaderBadProxyRemoved += (sender, id, proxy) =>
            {
                Console.WriteLine("D[" + id + "] Bad proxy remove " + proxy.Ip + ":" + proxy.Port + " from " +
                                  proxy.Country);
            };
            downloader.DownloaderNoGoodProxyLeft += sender =>
            {
                Console.WriteLine("No good proxies left, have to terminate!");
            };
        }

        public static List<IThreadedWebClientJob> Return(this IThreadedWebClientJob self, IThreadedWebClientJob job)
        {
            return new List<IThreadedWebClientJob>() { job };
        }
        public static List<IThreadedWebClientJob> ReturnNothing(this IThreadedWebClientJob self)
        {
            return new List<IThreadedWebClientJob>();
        }
    }
}
