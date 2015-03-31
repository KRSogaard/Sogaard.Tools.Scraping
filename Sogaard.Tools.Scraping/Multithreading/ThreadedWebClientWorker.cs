namespace Sogaard.Tools.Scraping.Multithreading
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public delegate void ThreadWorkStatusChangedEvent(object sender, int threadId, IThreadedWebClientJob job);
    public delegate void JobDoneEvent(object sender, int threadId, IThreadedWebClientJob job);
    public delegate void ErrorEvent(object sender, int threadId, IThreadedWebClientJob job, Exception exp);
    public delegate void WorkDoneEvent(object sender);

    /// <summary>
    /// Job based threaded WebClient worker
    /// This will generate two array of threads,
    /// one array of threads will download, the other
    /// will do the work on downloaded data.
    /// </summary>
    public class ThreadedWebClientWorker
    {
        /// <summary>
        /// Jobs yet to be done. FIFO
        /// </summary>
        private ConcurrentQueue<IThreadedWebClientJob> JobsQueue { get; set; }
        /// <summary>
        /// Use the stack instead of queue if this is true
        /// </summary>
        private bool useDepthFirst { get; set; }
        /// <summary>
        /// Jobs yet to be done. FILO
        /// </summary>
        private ConcurrentStack<IThreadedWebClientJob> JobsQueueStack { get; set; }

        /// <summary>
        /// Array of woker threads
        /// </summary>
        private Thread[] Threads { get; set; }
        /// <summary>
        /// Each theads will set their relative index
        /// to true, when there is no work for it to do.
        /// When all are true can the worker finish.
        /// </summary>
        private bool[] ThreadsDone { get; set; }
        /// <summary>
        /// Job based Threaded WebClient
        /// </summary>
        private ThreadedWebClientDownloader client { get; set; }
        /// <summary>
        /// When true must all threads shutdown.
        /// </summary>
        private bool closeThreads { get; set; }

        private bool isPaused { get; set; }

        public int BadJobsRetry { get; set; }

        public int BadProxyRetry { get; set; }

        #region Transfer from constructor to run variable
        private int workThreads;
        private int downloadThreads;
        private List<WebProxyHolder> proxies;
        #endregion

        #region Events
        public event ThreadWorkStatusChangedEvent WorkerThreadStatusChanged;
        public event JobDoneEvent WorkerThreadJobDone;
        public event ErrorEvent WorkerThreadError;
        public event JobInQueueEvent WorkerJobInQueueChanged;
        public event WorkDoneEvent WorkDone;

        public event DownloaderThreadJobChangedEvent DownloaderThreadJobChanged;
        public event DownloaderThreadStatusEvent DownloaderThreadStatus;
        public event DownloaderNoGoodProxyLeftEvent DownloaderNoGoodProxyLeft;
        public event DownloaderBadProxyEvent DownloaderBadProxyRemoved;

        public event JobProcessingEvent DownloaderJobProcessingChanged;
        public event JobInQueueEvent DownloaderJobInQueueChanged;
        public event JobDoneInQueueEvent DownloaderJobDoneInQueueChanged;
        #endregion

        /// <summary>
        /// Job based threaded WebClient worker
        /// This will generate two array of threads,
        /// one array of threads will download, the other
        /// will do the work on downloaded data.
        /// </summary>
        /// <param name="workThreads">Required amount of worker threads</param>
        /// <param name="downloadThreads">Required amount of download threads</param>
        /// <param name="initialJobs">Initial jobs to run</param>
        /// <param name="depthFirst">Use FILO instead of FIFO</param>
        /// <param name="proxies">Proxies to use</param>
        public ThreadedWebClientWorker(int maxDownloadQueue = 250, bool depthFirst = false)
        {
            this.client = new ThreadedWebClientDownloader(downloadThreads, maxDownloadQueue, depthFirst);
            this.BadJobsRetry = 4;
            this.BadProxyRetry = 2;
            this.useDepthFirst = depthFirst;
            
            if (this.useDepthFirst)
            {
                this.JobsQueueStack = new ConcurrentStack<IThreadedWebClientJob>();
            }
            else
            {
                this.JobsQueue = new ConcurrentQueue<IThreadedWebClientJob>();
            }
        }

        public void SetThreads(int workThreads, int downloadThreads)
        {
            this.workThreads = workThreads;
            this.downloadThreads = downloadThreads;
        }

        public void SetRetrys(int badJobRetry, int badProxyRetry)
        {
            this.BadJobsRetry = badJobRetry;
            this.BadProxyRetry = badProxyRetry;
        }

        public void AddJob(List<IThreadedWebClientJob> Jobs)
        {
            foreach (var job in Jobs)
            {
                AddJob(job);
            }
        }
        public void AddJob(IThreadedWebClientJob Job)
        {
            Enqueue(Job);
        }

        /// <summary>
        /// Add proxies for the download to use
        /// You can use this method many times
        /// </summary>
        public void AddProxies(List<WebProxyHolder> proxies)
        {
            this.client.AddProxies(proxies);
        }

        /// <summary>
        /// Start the jobs
        /// This method will block untill all work is done.
        /// </summary>
        public void Run()
        {
            this.client.SetThreads(this.downloadThreads);
            this.client.SetRetrys(this.BadJobsRetry, this.BadProxyRetry);

            this.client.DownloaderThreadJobChanged += OnDownloaderThreadJobChanged;
            this.client.DownloaderThreadStatus += OnDownloaderThreadStatus;
            this.client.DownloaderBadProxyRemoved += OnDownloaderBadProxyRemoved;
            this.client.DownloaderNoGoodProxyLeft += OnDownloaderNoGoodProxyLeft;
            this.client.JobProcessingChanged += OnDownloaderJobProcessingChanged;
            this.client.JobInQueueChanged += OnDownloaderJobInQueueChanged;
            this.client.JobDoneInQueueChanged += OnDownloaderJobDoneInQueueChanged;

            if (this.proxies != null)
            {
                this.client.AddProxies(proxies);
            }
            
            this.Threads = new Thread[workThreads];
            this.ThreadsDone = new bool[workThreads];
            for (int i = 0; i < workThreads; i++)
            {
                Threads[i] = new Thread(ThreadDownloadLoader);
                Threads[i].Start(i);
            }

            client.Start();
            Thread.Sleep(100);

            while (true)
            {
                int doneThreads = 0;
                foreach (var thread in this.Threads)
                {
                    if (!thread.IsAlive)
                    {
                        doneThreads++;
                    }
                }
                if (doneThreads == this.Threads.Length)
                {
                    break;
                }

                if (this.ThreadsDone.Where(x => x).Count() == this.ThreadsDone.Count())
                {
                    this.closeThreads = true;
                }
                Thread.Sleep(100);
            }
            this.client.Stop();
            if (this.WorkDone != null)
            {
                this.WorkDone(this);
            }
        }

        private void ThreadDownloadLoader(object threadIndexObject)
        {
            int threadIndex = (int)threadIndexObject;
            Console.WriteLine("Working Thread " + threadIndex + " started");

            while (!this.closeThreads)
            {
                try
                {
                    // If there are any jobs in our queue add them to the
                    // downloader.
                    IThreadedWebClientJob _job = this.DequeueJob();
                    if (_job != null)
                    {
                        this.client.AddJob(_job);
                    }
                    else
                    {
                        // Dequeue a job form the download
                        var job = this.client.GetJob();
                        // If there are no jobs avalible, and the download have no more jobs,
                        // and that there are no worker jobs, set the threads work status to
                        // no work.
                        if (job == null && this.client.JobsInQueue() == 0 && this.GetQueueCount() == 0)
                        {
                            this.ThreadsDone[threadIndex] = true;
                            if (this.WorkerThreadStatusChanged != null)
                                this.WorkerThreadStatusChanged(this, threadIndex, null);

                            Thread.Sleep(100);
                            continue;
                        }

                        // We did not get a job, but there are jobs in the worker or downloader
                        // queue
                        if (job == null)
                        {
                            // No job ready, but there are jobs waiting to be completed
                            Thread.Sleep(100);
                            continue;
                        }

                        // If we get to here, have we gotten a job to do
                        if (job != null)
                        {
                            // If we had set the worker as having no work, will we need to updated
                            // it, to ensure that they wont shut down.
                            if (this.ThreadsDone[threadIndex])
                            {
                                if (this.WorkerThreadStatusChanged != null)
                                    this.WorkerThreadStatusChanged(this, threadIndex, job);
                                this.ThreadsDone[threadIndex] = false;
                            }

                            try
                            {
                                // Execute the job and add the list new jobs to the queue
                                var newJobs = job.Execute();
                                foreach (var newJob in newJobs)
                                {
                                    this.Enqueue(newJob);
                                }

                                if (this.WorkerThreadJobDone != null)
                                {
                                    this.WorkerThreadJobDone(this, threadIndex, job);
                                }
                            }
                            catch (Exception exp)
                            {
                                // There where an uncaught error in the job
                                // inform the job and do not requeue it.
                                job.FailedExecute(exp);
                                if (WorkerThreadError != null)
                                {
                                    this.WorkerThreadError(this, threadIndex, job, exp);
                                }
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Worker Thread error: " + exp.Message);
                }
            }
        }

        private IThreadedWebClientJob DequeueJob()
        {
            IThreadedWebClientJob job;
            if (this.useDepthFirst)
            {
                if (this.JobsQueueStack.TryPop(out job))
                {
                    if (this.WorkerJobInQueueChanged != null)
                    {
                        this.WorkerJobInQueueChanged(this, this.useDepthFirst ? this.JobsQueueStack.Count : this.JobsQueue.Count);
                    }
                    return job;
                }
                return null;
            }
            if (this.JobsQueue.TryDequeue(out job))
            {
                if (this.WorkerJobInQueueChanged != null)
                {
                    this.WorkerJobInQueueChanged(this, this.useDepthFirst ? this.JobsQueueStack.Count : this.JobsQueue.Count);
                }
                return job;
            }
            return null;
        }

        private void Enqueue(IThreadedWebClientJob job)
        {
            if (this.useDepthFirst)
            {
                this.JobsQueueStack.Push(job);
            }
            else
            {
                this.JobsQueue.Enqueue(job);
            }
            if (this.WorkerJobInQueueChanged != null)
            {
                this.WorkerJobInQueueChanged(this, this.useDepthFirst ? this.JobsQueueStack.Count : this.JobsQueue.Count);
            }
        }

        private int GetQueueCount()
        {
            if (this.useDepthFirst)
            {
                return this.JobsQueueStack.Count;
            }
            return this.JobsQueue.Count;
        }

        public void Pause()
        {
            if (!isPaused)
            {
                this.isPaused = true;
                this.client.Stop();
            }
            else
            {
                this.isPaused = false;
                this.client.Start();
            }
        }

        public void Stop()
        {
            this.closeThreads = true;
            this.client.ClearJobs();
        }

        #region Events
        private void OnDownloaderThreadJobChanged(object sender, int threadId, IThreadedWebClientJob job)
        {
            if (this.DownloaderThreadJobChanged != null)
            {
                this.DownloaderThreadJobChanged(sender, threadId, job);
            }
        }

        private void OnDownloaderThreadStatus(object sender, int threadId, bool status)
        {
            if (this.DownloaderThreadStatus != null)
            {
                this.DownloaderThreadStatus(sender, threadId, status);
            }
        }
        private void OnDownloaderBadProxyRemoved(object sender, int threadId, WebProxyHolder proxy)
        {
            if (this.DownloaderBadProxyRemoved != null)
            {
                this.DownloaderBadProxyRemoved(sender, threadId, proxy);
            }
        }

        private void OnDownloaderNoGoodProxyLeft(object sender)
        {
            // The webclient is not going to do any more work,
            // so we have to shut down.
            closeThreads = true;

            if (this.DownloaderNoGoodProxyLeft != null)
            {
                this.DownloaderNoGoodProxyLeft(sender);
            }
        }

        private void OnDownloaderJobDoneInQueueChanged(object sender, int jobs)
        {
            if (this.DownloaderJobDoneInQueueChanged != null)
            {
                this.DownloaderJobDoneInQueueChanged(sender, jobs);
            }
        }

        private void OnDownloaderJobInQueueChanged(object sender, int jobs)
        {
            if (this.DownloaderJobInQueueChanged != null)
            {
                this.DownloaderJobInQueueChanged(sender, jobs);
            }
        }

        private void OnDownloaderJobProcessingChanged(object sender, int currentProcessing)
        {
            if (this.DownloaderJobProcessingChanged != null)
            {
                this.DownloaderJobProcessingChanged(sender, currentProcessing);
            }
        }
        #endregion

        public void ClearJobs()
        {
            if (this.JobsQueueStack != null)
                this.JobsQueueStack.Clear();
            if(this.JobsQueue != null)
                this.JobsQueue = new ConcurrentQueue<IThreadedWebClientJob>();
            if(this.client != null)
                this.client.ClearJobs();
        }

        public int GetWorkerThreadCount()
        {
            return this.workThreads;
        }

        public int GetDownloadThreadCount()
        {
            return this.downloadThreads;
        }

        public List<WebProxyHolder> GetProxies()
        {
            return this.client.GetProxies();
        }
    }
}