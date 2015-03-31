using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping.Multithreading.GUI
{
    public static class ScraperGUIHelper
    {
        public static void AttachGui(ThreadedWebClientWorker worker)
        {
            var task = new Thread(() =>
            {
                System.Windows.Application application = new System.Windows.Application();
                application.Run(new DownloadManagerGuiWindow(worker));
            });
            task.SetApartmentState(ApartmentState.STA);

            worker.WorkDone += sender =>
            {
                try
                {
                    task.Abort();
                }
                catch (Exception)
                {
                }
            };
            task.Start();
        }
    }
}
