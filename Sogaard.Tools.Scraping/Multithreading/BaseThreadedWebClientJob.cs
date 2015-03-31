using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading
{
    public abstract class BaseThreadedWebClientJob : IThreadedWebClientJob
    {
        private string Url;
        private string Html;
        private Exception exception;

        protected void SetUrl(string url)
        {
            this.Url = url;
        }
        protected string GetUrl()
        {
            return this.Url;
        }

        protected string GetHtml()
        {
            return this.Html;
        }
        protected void SetHtml(string value)
        {
            this.Html = value;
        }

        public Exception GetException()
        {
            return this.exception;
        }

        public abstract List<IThreadedWebClientJob> Execute();
        public abstract void FailedExecute(Exception exp);

        public virtual async Task ExecuteDownload(HttpClient client, CancellationToken cancelToken)
        {
            if (!this.CanDownload())
            {
                return;
            }

            ScraperHelper.SetOrigenToClient(this.Url, client);
            Console.WriteLine("Downloading: " + this.Url);
            var result = await client.GetAsync(Url, cancelToken).ConfigureAwait(false);
            if (result.IsSuccessStatusCode)
            {
                var html = await result.Content.ReadAsStringAsync().WithCancellation(cancelToken).ConfigureAwait(false);
                if (!this.ValidateHtml(html))
                {
                    throw new HttpRequestException("HTML returned was not verified.");
                }
                this.Html = html;
            }
            else
            {
                throw new HttpRequestException("HTML returned was not verified.");
            }

        }
        public virtual void FailedDownload(Exception exp)
        {
            this.exception = exp;
        }

        protected virtual bool CanDownload()
        {
            return true;
        }
        protected abstract bool ValidateHtml(string html);

    }
}
