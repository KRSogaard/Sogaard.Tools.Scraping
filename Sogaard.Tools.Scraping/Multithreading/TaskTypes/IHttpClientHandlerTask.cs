using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.TaskTypes
{
    public interface IHttpClientHandlerTask
    {
        HttpClientHandler GetHttpClient(WebProxy webProxy);
    }
}
