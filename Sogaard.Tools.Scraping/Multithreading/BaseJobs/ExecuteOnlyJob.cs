using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.BaseJobs
{
    public abstract class ExecuteOnlyJob : IThreadedWebClientJob
    {
        public abstract List<IThreadedWebClientJob> Execute();
        public abstract void FailedExecute(Exception exp);

        public async Task ExecuteDownload(HttpClient client, CancellationToken cancelToken)
        {
            return;
        }
        public void FailedDownload(Exception exp)
        {
            return;
        }

    }
}
