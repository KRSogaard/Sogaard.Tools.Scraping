using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading
{
    using System;
    using System.Collections.Generic;

    public interface IThreadedWebClientJob
    {
        List<IThreadedWebClientJob> Execute();
        void FailedExecute(Exception exp);

        Task ExecuteDownload(HttpClient client, CancellationToken cancelToken);
        void FailedDownload(Exception exp);
    }
}
