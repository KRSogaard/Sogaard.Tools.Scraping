using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.Testing
{
    public class SingleThreadTest
    {
        public async void TestJob(IThreadedWebClientJob job)
        {
            using (HttpClient client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                // Add default headers to the client to simulate 
                // a real browser
                ScraperHelper.AddHeadersToClient(client);

                try
                {
                    CancellationTokenSource cancelToken = new CancellationTokenSource();
                    cancelToken.CancelAfter(new TimeSpan(1, 0, 0, 30));
                    await job.ExecuteDownload(client, cancelToken.Token);

                    List<IThreadedWebClientJob> newJobs;
                    try
                    {
                        newJobs = job.Execute();
                        foreach (var t in newJobs)
                        {
                            TestJob(t);
                        }
                    }
                    catch (Exception exp)
                    {
                        job.FailedExecute(exp);
                    }
                }
                // WebException may be a proxy error
                catch (WebException exp)
                {
                    throw;
                }
                // Uncaught error
                catch (Exception exp)
                {
                    job.FailedDownload(exp);
                }
            }
        } 
    }
}
