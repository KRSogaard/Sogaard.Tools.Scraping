using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.TaskTypes
{
    public abstract class SpanTask : ITypedTask, IThreadedWebClientJob
    {
        private static Dictionary<string, DateTime> LastRun = new Dictionary<string, DateTime>();

        private string id;
        private TimeSpan span;

        public SpanTask(string id, TimeSpan span)
        {
            this.id = id;
            this.span = span;
        }

        public bool Verify()
        {
            if (this.id == null || this.span == null)
            {
                throw new Exception("Id or Span is missing");
            }

            lock (LastRun)
            {
                if (LastRun.ContainsKey(id))
                {
                    return LastRun[id] + this.span <= DateTime.Now;
                }
            }
            return true;
        }

        public abstract List<IThreadedWebClientJob> Execute();

        public abstract void FailedExecute(Exception exp);

        public virtual Task ExecuteDownload(System.Net.Http.HttpClient client, System.Threading.CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {
                lock (LastRun)
                {
                    if (LastRun.ContainsKey(id))
                    {
                        LastRun[id] = DateTime.Now;
                    }
                    else
                    {
                        LastRun.Add(id, DateTime.Now);
                    }
                }
            });
        }

        public abstract void FailedDownload(Exception exp);
    }
}
