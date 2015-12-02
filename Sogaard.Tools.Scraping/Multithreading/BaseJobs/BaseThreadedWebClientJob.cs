﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.BaseJobs
{
    public abstract class BaseThreadedWebClientJob : IThreadedWebClientJob
    {
        private string Url;
        private string Html;

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
        
        public abstract List<IThreadedWebClientJob> Execute();

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
                throw new HttpRequestException("HTML request was not successfull.");
            }

        }

        public virtual void FailedDownload(Exception exp)
        {
        }

        public virtual void FailedExecute(Exception exp)
        {

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
