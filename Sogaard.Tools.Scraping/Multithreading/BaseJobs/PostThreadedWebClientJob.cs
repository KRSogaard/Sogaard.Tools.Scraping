using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.BaseJobs
{
    public abstract class PostThreadedWebClientJob : BaseThreadedWebClientJob
    {
        private HttpResponseHeaders responseHeaders;
        private List<KeyValuePair<string, string>> formData;
        private string formString;

        public HttpResponseHeaders GetResponseHeaders()
        {
            return this.responseHeaders;
        }

        public void AddForm(string key, string value)
        {
            if (this.formData == null)
            {
                this.formData = new List<KeyValuePair<string, string>>();
            }

            this.formData.Add(new KeyValuePair<string, string>(key, value));
        }

        public void AddForm(string key, IEnumerable<string> values)
        {
            foreach (var v in values)
            {
                this.AddForm(key, v);
            }
        }

        public void SetForm(string value)
        {
            this.formString = value;
        }
        
        public override async Task ExecuteDownload(HttpClient client, CancellationToken cancelToken)
        {
            var uri = new Uri(this.GetUrl());
            client.BaseAddress = this.GetUrl().Contains("https:") ? new Uri("https://" + uri.Host) : new Uri("http://" + uri.Host);

            var message = new HttpRequestMessage(HttpMethod.Post, uri.LocalPath);
            if (this.headers != null)
            {
                foreach (var header in this.headers)
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            var keyValue = new List<KeyValuePair<string, string>>();
            if (this.formData != null)
            {
                keyValue.AddRange(this.formData);
            }
            if (this.formString != null)
            {
                message.Content = new StringContent(this.formString, Encoding.UTF8, "application/json");
            }
            else
            {

                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> current in keyValue)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append('&');
                    }

                    stringBuilder.Append(Encode(current.Key));
                    stringBuilder.Append('=');
                    stringBuilder.Append(Encode(current.Value));
                }

                message.Content = new StringContent(stringBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
                //message.Content = new MyFormUrlEncodedContent(keyValue);
            }

            var result = await client.SendAsync(message, cancelToken).ConfigureAwait(false);

            this.responseHeaders = result.Headers;
            if (result.IsSuccessStatusCode)
            {
                var html = await result.Content.ReadAsStringAsync().WithCancellation(cancelToken).ConfigureAwait(false);
                if (!this.ValidateHtml(html))
                {
                    throw new HttpRequestException("HTML returned was not verified.");
                }
                this.SetHtml(html);
            }
            else
            {
                throw new HttpRequestException("HTML returned was not verified.");
            }
        }

        private static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }
            return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
        }
        
        public class MyFormUrlEncodedContent : ByteArrayContent
        {
            public MyFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
                : base(MyFormUrlEncodedContent.GetContentByteArray(nameValueCollection))
            {
                base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            }

            private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
            {
                if (nameValueCollection == null)
                {
                    throw new ArgumentNullException("nameValueCollection");
                }
                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> current in nameValueCollection)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append('&');
                    }

                    stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Key));
                    stringBuilder.Append('=');
                    stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Value));
                }
                return Encoding.Default.GetBytes(stringBuilder.ToString());
            }
            private static string Encode(string data)
            {
                if (string.IsNullOrEmpty(data))
                {
                    return string.Empty;
                }
                return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
            }
        }
    }
}
