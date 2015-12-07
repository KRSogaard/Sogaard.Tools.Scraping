using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.BaseJobs
{
    public abstract class BaseThreadedWebClientJob : IThreadedWebClientJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string Url;
        private string Html;
        private Dictionary<string, string> Headers;

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

        protected void AddHeader(string key, string value)
        {
            if (Headers == null)
            {
                this.Headers = new Dictionary<string, string>();
            }
            this.Headers.Add(key, value);
        }
        
        public abstract List<IThreadedWebClientJob> Execute();

        public virtual async Task ExecuteDownload(HttpClient client, CancellationToken cancelToken)
        {
            if (!this.CanDownload())
            {
                logger.Debug("Can download returned false, stopping the download job.");
                return;
            }

            ScraperHelper.SetOrigenToClient(this.Url, client);
            if (this.Headers != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in Headers)
                {
                    client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            logger.Trace("Downloading: " + this.Url);
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
                throw new HttpRequestException("HTML request was not successfull.");
            }
        }

        public virtual void FailedDownload(Exception exp)
        {
            logger.Error("Download failed", exp);
        }

        public virtual void FailedExecute(Exception exp)
        {
            logger.Error("Execute failed", exp);
        }

        protected virtual bool CanDownload()
        {
            return true;
        }
        protected virtual bool ValidateHtml(string html)
        {
            return true;
        }
    }
}
