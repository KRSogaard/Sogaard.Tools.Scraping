using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sogaard.Scraper.HoursGuid.GUI.ViewModels;

namespace Sogaard.Tools.Scraping.Multithreading.GUI
{
    public delegate void JobsDoneEvent();

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DownloadManagerGuiWindow : Window
    {
        private ThreadedWebClientWorker worker;
        private MainViewModel viewModel;
        private List<IThreadedWebClientJob> initialJobs;
        private bool running = false;
        private bool paused = false;
        private int jobRetrys;
        private int proxyRetry;
        private bool autoStart;
        private bool closeOnDone;

        public event JobsDoneEvent JobsDoneEvent;

        public DownloadManagerGuiWindow(ThreadedWebClientWorker worker)
        {
            InitializeComponent();
            this.viewModel = new MainViewModel();
            this.DataContext = this.viewModel;

            this.viewModel.WorkThreadCount = 2;
            this.viewModel.DownloadThreadCount = 50;
            this.viewModel.DownloadThreadCount = worker.GetDownloadThreadCount();
            this.viewModel.WorkThreadCount = worker.GetWorkerThreadCount();

            this.worker = worker;
        }

        public void AddProxies(List<WebProxyHolder> proxies)
        {
            this.worker.AddProxies(proxies);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < viewModel.WorkThreadCount; i++)
            {
                this.viewModel.WorkThreads.Add(new ThreadViewModel(i)
                {
                    Status = "Closed"
                });
            }
            for (int i = 0; i < viewModel.DownloadThreadCount; i++)
            {
                this.viewModel.DownloadThreads.Add(new ThreadViewModel(i)
                {
                    Status = "Closed"
                });
            }
            foreach (var proxy in worker.GetProxies())
            {
                this.viewModel.Proxies.Add(new ProxyViewModel(proxy));
            }
            // Register the events
            worker.WorkerThreadStatusChanged += OnWorkerThreadStatusChanged;
            worker.WorkerThreadJobDone += OnWorkerThreadJobDone;
            worker.WorkerThreadError += OnWorkerThreadError;
            worker.DownloaderThreadJobChanged += OnDownloaderThreadJobChanged;
            worker.DownloaderThreadStatus += OnDownloaderThreadStatus;
            worker.DownloaderJobDoneInQueueChanged += OnDownloaderJobDoneInQueueChanged;
            worker.DownloaderJobInQueueChanged += OnDownloaderJobInQueueChanged;
            worker.DownloaderJobProcessingChanged += OnDownloaderJobProcessingChanged;
            worker.WorkerJobInQueueChanged += OnWorkerJobInQueueChanged;
            worker.DownloaderBadProxyRemoved += OnDownloaderBadProxyRemoved;
        }

        #region Events and viewmodel update
        private void OnWorkerThreadError(object sender, int threadId, IThreadedWebClientJob job, Exception exp)
        {
            SetWorkerThreadStatus(threadId, exp.Message);
        }
        private void OnWorkerThreadJobDone(object sender, int threadId, IThreadedWebClientJob job)
        {
            SetWorkerThreadStatus(threadId, "Finished: " + job);
            SetWorkerJobDone();
        }
        private void OnWorkerThreadStatusChanged(object sender, int threadId, IThreadedWebClientJob job)
        {
            if (job == null)
            {
                SetWorkerThreadStatus(threadId, "Inactive");
            }
            else
            {
                SetWorkerThreadStatus(threadId, "Running: " + job);
            }
        }
        private void OnWorkerJobInQueueChanged(object sender, int jobs)
        {
            this.SetWorkerJobsInQueue(jobs);
        }

        private void OnDownloaderJobProcessingChanged(object sender, int currentProcessing)
        {
            this.SetDownloaderJobProcessing(currentProcessing);
        }
        private void OnDownloaderJobInQueueChanged(object sender, int jobs)
        {
            this.SetDownloaderJobInQueue(jobs);
        }
        private void OnDownloaderJobDoneInQueueChanged(object sender, int jobs)
        {
            this.SetDownloaderJobDoneInQueue(jobs);
        }
        private void OnDownloaderThreadStatus(object sender, int threadId, bool status)
        {
            if (!status)
            {
                SetDownloadThreadStatus(threadId, "Closed");
            }
            else
            {
                SetDownloadThreadStatus(threadId, "Active");
            }
        }
        private void OnDownloaderThreadJobChanged(object sender, int threadId, IThreadedWebClientJob job)
        {
            if (job == null)
            {
                SetDownloadThreadStatus(threadId, "Inactive");
                return;
            }
            SetDownloadThreadStatus(threadId, job.ToString());
        }
        private void OnDownloaderBadProxyRemoved(object sender, int threadId, WebProxyHolder proxy)
        {
            this.SetRemovedProxy(proxy);
        }

        private void SetWorkerJobsInQueue(int value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.WorkJobsInQueue = value;
            }
            else
            {
                SetIntCallback d = new SetIntCallback(SetWorkerJobsInQueue);
                this.Dispatcher.Invoke(d, new object[] { value });
            }
        }
        private void SetDownloaderJobProcessing(int value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.DownloadJobsCurrentlyProcessing = value;
            }
            else
            {
                SetIntCallback d = new SetIntCallback(SetDownloaderJobProcessing);
                this.Dispatcher.Invoke(d, new object[] { value });
            }
        }
        private void SetDownloaderJobInQueue(int value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.DownloadJobsInQueue = value;
            }
            else
            {
                SetIntCallback d = new SetIntCallback(SetDownloaderJobInQueue);
                this.Dispatcher.Invoke(d, new object[] { value });
            }
        }
        private void SetDownloaderJobDoneInQueue(int value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.AwaitingDownDownloadJobs = value;
            }
            else
            {
                SetIntCallback d = new SetIntCallback(SetDownloaderJobDoneInQueue);
                this.Dispatcher.Invoke(d, new object[] { value });
            }
        }
        private void SetWorkerThreadStatus(int index, string value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.SetWorkStatus(index, value);
            }
            else
            {
                SetTextCallback d = new SetTextCallback(SetWorkerThreadStatus);
                this.Dispatcher.Invoke(d, new object[] { index, value });
            }
        }
        private void SetDownloadThreadStatus(int index, string value)
        {
            if (value.Contains("Object"))
            {
                value = "Inactive";
            }
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.SetDownloadStatus(index, value);
            }
            else
            {
                SetTextCallback d = new SetTextCallback(SetDownloadThreadStatus);
                this.Dispatcher.Invoke(d, new object[] { index, value });
            }
        }
        private void SetRemovedProxy(WebProxyHolder proxy)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.viewModel.SetProxyAsRemoved(proxy);
            }
            else
            {
                SetWebHolderCallback d = new SetWebHolderCallback(SetRemovedProxy);
                this.Dispatcher.Invoke(d, new object[] { proxy });
            }
        }

        private object jobsDoneLocker = new object();
        private void SetWorkerJobDone()
        {
            if (this.Dispatcher.CheckAccess())
            {
                lock (jobsDoneLocker)
                {
                    this.viewModel.CompletedJobs++;
                }
            }
            else
            {
                SetVoidCallback d = new SetVoidCallback(SetWorkerJobDone);
                this.Dispatcher.Invoke(d);
            }
        }

        private void SetProgressbar(bool on)
        {
            if (ProgressBar.Dispatcher.CheckAccess())
            {
                ProgressBar.Visibility = on ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                SetBoolCallback d = new SetBoolCallback(SetProgressbar);
                ProgressBar.Dispatcher.Invoke(d, new object[] { on });
            }
        }

        protected virtual void OnJobsDoneEvent()
        {
            var handler = JobsDoneEvent;
            if (handler != null) handler();
        }


        public delegate void SetTextCallback(int index, string value);
        public delegate void SetIntCallback(int value);
        public delegate void SetBoolCallback(bool value);
        public delegate void SetWebHolderCallback(WebProxyHolder value);
        public delegate void SetVoidCallback();
        #endregion
    }
}
