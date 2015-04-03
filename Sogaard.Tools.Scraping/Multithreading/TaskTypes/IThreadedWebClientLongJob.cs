using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.TaskTypes
{
    public interface IThreadedWebClientLongJob
    {
        TimeSpan GetTimeOut();
    }
}
