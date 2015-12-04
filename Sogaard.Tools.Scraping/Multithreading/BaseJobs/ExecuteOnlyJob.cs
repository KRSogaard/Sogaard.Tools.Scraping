using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Sogaard.Tools.Scraping.Multithreading.BaseJobs
{
    public abstract class ExecuteOnlyJob : IThreadedWebClientJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public abstract List<IThreadedWebClientJob> Execute();

        public void FailedExecute(Exception exp)
        {
            logger.Error("Execute failed", exp);
        }

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
