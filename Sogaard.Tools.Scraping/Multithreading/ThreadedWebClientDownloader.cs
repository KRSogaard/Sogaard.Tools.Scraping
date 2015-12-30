using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NLog;

namespace Sogaard.Tools.Scraping.Multithreading
{
    using Sogaard.Tools.Scraping.Multithreading.TaskTypes;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    
    public delegate void DownloaderThreadJobChangedEvent(object sender, int threadId, IThreadedWebClientJob job);
    public delegate void DownloaderThreadStatusEvent(object sender, int threadId, bool status);
    public delegate void DownloaderNoGoodProxyLeftEvent(object sender);
    public delegate void DownloaderBadProxyEvent(object sender, int threadId, WebProxyHolder proxy);
    public delegate void DownloaderBadJobEvent(object sender, int threadId, IThreadedWebClientJob job);
    public delegate void JobProcessingEvent(object sender, int currentProcessing);
    public delegate void JobInQueueEvent(object sender, int jobs);
    public delegate void JobDoneInQueueEvent(object sender, int jobs);

    /// <summary>
    /// This is a job based multi threaded web client
    /// </summary>
    public class ThreadedWebClientDownloader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Jobs to be executed. FIFO
        /// </summary>
        private ConcurrentQueue<IThreadedWebClientJob> jobsQueue;
        /// <summary>
        /// Use the stack instead of queue if this is true
        /// </summary>
        private bool useDepthFirst;
        /// <summary>
        /// Jobs to be executed. FILO
        /// </summary>
        private ConcurrentStack<IThreadedWebClientJob> JobsQueueStack;
        /// <summary>
        /// Done jobs ready to be returned
        /// </summary>
        private ConcurrentQueue<IThreadedWebClientJob> doneJobQueue;
        /// <summary>
        /// Voting for bad jobs
        /// </summary>
        private ConcurrentDictionary<IThreadedWebClientJob, int> badJobs;
        /// <summary>
        /// Time to retry a job before it is removed
        /// </summary>
        private int badJobRetry;

        /// <summary>
        /// Queue of proxies to be used
        /// </summary>
        private ConcurrentQueue<WebProxyHolder> proxies;
        /// <summary>
        /// Bad proxy voting, if the proxy returns a bad result
        /// add 1 do the proxy here
        /// </summary>
        private ConcurrentDictionary<WebProxyHolder, int> badProxy;
        /// <summary>
        /// The amount of votes required to remove a bad proxy
        /// </summary>
        private int votesToRemoveProxy;
        /// <summary>
        /// Indicate if the no proxies left event have been fired,
        /// to ensure it is not send multible times.
        /// </summary>
        private bool NoGoodProxyEventFired;
        /// <summary>
        /// Will the webclient use proxies
        /// </summary>
        private bool useProxies;

        /// <summary>
        /// The number of jobs currently being processed.
        /// </summary>
        private int jobsInProcess;
        private object jobsInProcessLocker = new object();

        /// <summary>
        /// The download threads
        /// </summary>
        private Thread[] threads;
        /// <summary>
        /// If this is true, must all threads stop all work
        /// </summary>
        private bool stopThread;

        /// <summary>
        /// Number of threads, used to transfer from the counstructor
        /// </summary>
        private int numberOfThreads;

        /// <summary>
        /// If the amount of jobs in the done queue exceeds this number
        /// Halt all download untill the done queue is empty
        /// </summary>
        private int maxDoneQueue;

        /// <summary>
        /// Waiting for an empty done queue
        /// </summary>
        private bool waitingForEmpty;

        /// <summary>
        /// Kill the download if there are no proxies left
        /// </summary>
        private bool dieOnProxiesLeft;

        #region Events
        public event DownloaderThreadJobChangedEvent DownloaderThreadJobChanged;
        public event DownloaderThreadStatusEvent DownloaderThreadStatus;
        public event DownloaderNoGoodProxyLeftEvent DownloaderNoGoodProxyLeft;
        public event DownloaderBadProxyEvent DownloaderBadProxyRemoved;
        public event DownloaderBadJobEvent DownloaderBadJob;

        public event JobProcessingEvent JobProcessingChanged;
        public event JobInQueueEvent JobInQueueChanged;
        public event JobDoneInQueueEvent JobDoneInQueueChanged;
        #endregion

        /// <summary>
        /// This is a job based multi threaded web client
        /// </summary>
        /// <param name="numberOfThreads"></param>
        public ThreadedWebClientDownloader(int numberOfThreads, int maxDoneQueue = 250, bool depthFirst = false)
        {
            this.useDepthFirst = depthFirst;
            this.votesToRemoveProxy = 2;
            this.badJobRetry = 4;
            this.numberOfThreads = numberOfThreads;
            this.maxDoneQueue = maxDoneQueue;
            this.waitingForEmpty = false;
            this.dieOnProxiesLeft = true;

            this.doneJobQueue = new ConcurrentQueue<IThreadedWebClientJob>();
            this.proxies = new ConcurrentQueue<WebProxyHolder>();
            this.badProxy = new ConcurrentDictionary<WebProxyHolder, int>();

            if(this.useDepthFirst)
                this.JobsQueueStack = new ConcurrentStack<IThreadedWebClientJob>();
            else
                this.jobsQueue = new ConcurrentQueue<IThreadedWebClientJob>();

            logger.Debug("ThreadedWebClientDownloader:\n    useDepthFirst: {0}\n    votesToRemoveProxy: {1}\n    badJobRetry: {2}\n    maxDoneQueue: {3}\n    waitingForEmpty: {4}", useDepthFirst, votesToRemoveProxy, badJobRetry, maxDoneQueue, waitingForEmpty);
        }

        public void SetThreads(int numberOfThreads)
        {
            logger.Trace("Download threads have been set to {0}", numberOfThreads);
            this.numberOfThreads = numberOfThreads;
        }

        public void SetRetrys(int badJobRetry, int badProxyVote)
        {
            logger.Trace("Retrys have been set to Proxy: {0}, Job: {1}", badProxyVote, badJobRetry);
            this.votesToRemoveProxy = badProxyVote;
            this.badJobRetry = badJobRetry;
        }

        /// <summary>
        /// Start all the threads
        /// This will not block, you will have to call Stop to shutdown the threads
        /// </summary>
        public void Start()
        {
            logger.Debug("Starting {0} download threads.", this.numberOfThreads);
            // Rests flags
            this.badJobs = new ConcurrentDictionary<IThreadedWebClientJob, int>();
            this.NoGoodProxyEventFired = false;
            this.stopThread = false;

            // Create new threads
            this.threads = new Thread[this.numberOfThreads];
            for (int i = 0; i < this.numberOfThreads; i++)
            {
                this.threads[i] = new Thread(this.RunWebDownload);
                this.threads[i].Start(i);
            }
        }

        /// <summary>
        /// Make all threads stop work
        /// </summary>
        public void Stop()
        {
            logger.Debug("Stopping all download threads.");
            this.stopThread = true;

            //TODO: Wait untill all threads have stopped.
        }

        /// <summary>
        /// Add proxies to be used
        /// </summary>
        /// <param name="proxies"></param>
        public void AddProxies(List<WebProxyHolder> proxies)
        {
            foreach (var p in proxies)
            {
                logger.Trace("Got new proxy {0}", p);
                this.proxies.Enqueue(p);
            }
            this.useProxies = this.proxies.Count > 0;
        }

        /// <summary>
        /// Set the download to require proxies
        /// </summary>
        public void ForceProxiesOnly()
        {
            this.useProxies = true;
        }

        public void DieOnNoProxies(bool value)
        {
            this.dieOnProxiesLeft = value;
        }

        /// <summary>
        /// Add a job to be executed
        /// </summary>
        public void AddJob(IThreadedWebClientJob job)
        {
            logger.Trace("Adding download job {0}", job);
            this.Enqueue(job);
        }

        /// <summary>
        /// Return the count of jobs to be executed,
        /// plus the amount of jobs already being 
        /// processed.
        /// </summary>
        /// <returns></returns>
        public int JobsInQueue()
        {
            if (this.useDepthFirst)
                return this.JobsQueueStack.Count + this.jobsInProcess;
            return this.jobsQueue.Count + this.jobsInProcess;
        }

        public IThreadedWebClientJob GetJob()
        {
            IThreadedWebClientJob job;
            if (this.doneJobQueue.TryDequeue(out job))
            {
                if (this.JobDoneInQueueChanged != null)
                {
                    this.JobDoneInQueueChanged(this, this.doneJobQueue.Count);
                }
                return job;
            }
            return null;
        }

        private async void RunWebDownload(object threadIndexObject)
        {
            int threadIndex = (int) threadIndexObject;
            logger.Trace("Download Thread {0} started", threadIndex);
            
            DateTime currentTaskStarted;
            Task currentTask;

            // Run untill asked to shut down
            while (!this.stopThread)
            {
                if (this.DownloaderThreadStatus != null)
                {
                    this.DownloaderThreadStatus(this, threadIndex, true);
                }

                // Done jobs queue full, halt download work
                if (this.doneJobQueue.Count >= this.maxDoneQueue)
                {
                    this.DownloaderThreadJobChanged(this, threadIndex, null);
                    this.waitingForEmpty = true;
                    await Task.Delay(50);
                    continue;
                }
                if (this.waitingForEmpty)
                {
                    this.DownloaderThreadJobChanged(this, threadIndex, null);
                    // If the jobs queue is empty, resume work
                    if (this.doneJobQueue.Count == 0)
                    {
                        this.waitingForEmpty = false;
                    }
                    await Task.Delay(50);
                    continue;
                }

                // Find a proxy to use, if required, if the method
                // returns false, do we have to stop work, and we
                // use proxies, but there are no proxies left to work with.
                WebProxyHolder proxy;
                IThreadedWebClientJob job;
                try
                {
                    // Dequeue a job.
                    job = this.Dequeue();
                    if (job != null)
                    {
                        logger.Trace("Downloader {0} got a new job {1}", threadIndex, job);
                        lock (this.jobsInProcessLocker)
                        {
                            this.jobsInProcess++;
                            if (this.JobProcessingChanged != null)
                            {
                                this.JobProcessingChanged(this, this.jobsInProcess);
                            }
                        }

                        if (this.DownloaderThreadJobChanged != null)
                        {
                            this.DownloaderThreadJobChanged(this, threadIndex, job);
                        }

                        WebProxy webProxy = this.HandleAddProxy(out proxy);
                        if (webProxy == null && this.useProxies)
                        {
                            if (this.dieOnProxiesLeft)
                            {
                                logger.Error("Download {0} was unable to find any proxies, shutting down.", threadIndex);
                                // No proxies to use, and we have to use proxies, kill all downloads.
                                this.stopThread = true;
                                this.jobsInProcess = 0; // Ensure worker is not stuck
                                return;
                            }
                            else
                            {
                                logger.Trace("Download {0} was unable to find any proxies, requeueing the job.", threadIndex);
                                // We just keep running untill we get proxies again.
                                // Requeue the job
                                this.Enqueue(job);
                                // Count down the working jobs
                                lock (this.jobsInProcessLocker)
                                {
                                    this.jobsInProcess--;
                                    if (this.JobProcessingChanged != null)
                                    {
                                        this.JobProcessingChanged(this, this.jobsInProcess);
                                    }
                                }
                                await Task.Delay(50);
                                continue;
                            }
                        }
                        try
                        {
                            bool run = true;
                            if (job is ITypedTask)
                            {
                                logger.Trace("Downloader {0}'s job {1} requires verification.", threadIndex, job);
                                run = ((ITypedTask)job).Verify();
                            }

                            if (run)
                            {
                                var HttpClientHandler = new HttpClientHandler()
                                {
                                    UseCookies = false,
                                    Proxy = webProxy,
                                    UseProxy = this.useProxies,
                                    // For Fiddler debugging
                                    //Proxy = new WebProxy("http://127.0.0.1:8888"),
                                    //UseProxy = true,
                                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                                };
                                if (job is IHttpClientHandlerTask)
                                {
                                    logger.Trace("Downloader {0}'s job {1} have custom HttpClient.", threadIndex, job);
                                    HttpClientHandler = ((IHttpClientHandlerTask) job).GetHttpClient(webProxy);
                                }
                                using (HttpClient client = new HttpClient(HttpClientHandler))
                                {
                                    // Add default headers to the client to simulate 
                                    // a real browser
                                    ScraperHelper.AddHeadersToClient(client);

                                    logger.Trace("Downloader {0} is running job {1}.", threadIndex, job);
                                    CancellationTokenSource cancelToken = new CancellationTokenSource();
                                    var timelimit = new TimeSpan(0, 0, 30);
                                    // Some jobs might requere a bigger timelimit
                                    if (job is IThreadedWebClientLongJob)
                                    {
                                        logger.Debug("Fetching timelimit for downloader {0}'s job {1}.", threadIndex,
                                            job);
                                        timelimit = ((IThreadedWebClientLongJob) job).GetTimeOut();
                                        logger.Debug("Downloader {0}'s job {1} have set a custome time limit to {2}.",
                                            threadIndex, job, timelimit);
                                    }

                                    cancelToken.CancelAfter(timelimit);
                                    logger.Trace("Downloader {0} is executing job {1}.", threadIndex, job);
                                    await job.ExecuteDownload(client, cancelToken.Token);
                                    logger.Trace("Downloader {0} is done executing job {1}.", threadIndex, job);

                                    doneJobQueue.Enqueue(job);
                                    if (this.JobDoneInQueueChanged != null)
                                    {
                                        this.JobDoneInQueueChanged(this, this.doneJobQueue.Count);
                                    }

                                    // Vote up good proxy, if have bad votes
                                    if (proxy != null && this.badProxy.ContainsKey(proxy))
                                    {
                                        logger.Trace("Proxy {0} was good, up voting it.", proxy);
                                        if (this.badProxy[proxy] > 0)
                                        {
                                            this.badProxy[proxy]--;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                logger.Trace("Downloader {0}'s job {1} did not verify, reenqueueing the job.", threadIndex, job);
                                // Requery the job
                                this.Enqueue(job);
                                await Task.Delay(10);
                            }
                        }
                        // WebException may be a proxy error
                        catch (HttpRequestException exp)
                        {
                            logger.Warn(exp, "Got web exception while executing downloader {0}'s job {1}.", threadIndex, job);
                            // Handle bad proxy voting
                            this.HandleBadProxy(threadIndex, proxy);
                            // Requeue the job
                            this.HandleBadJob(job, threadIndex);
                        }
                        // Uncaught error
                        catch (Exception exp)
                        {
                            logger.Error(exp, "Got unknown exception while executing downloader {0}'s job {1}.", threadIndex, job);
                            try
                            {
                                logger.Trace(exp, "Downloader {0} is running failed download for job {1}: {2}", threadIndex, job, exp.Message);
                                job.FailedDownload(exp);
                            }
                            catch (Exception exp2)
                            {
                                // Error here, nothing we can do
                            }
                            this.HandleBadJob(job, threadIndex);
                        }

                        lock (this.jobsInProcessLocker)
                        {
                            this.jobsInProcess--;
                            if (this.JobProcessingChanged != null)
                            {
                                this.JobProcessingChanged(this, this.jobsInProcess);
                            }
                        }
                    }
                    else
                    {
                        // Currently no jobs to do
                        if (this.DownloaderThreadJobChanged != null)
                        {
                            this.DownloaderThreadJobChanged(this, threadIndex, null);
                        }
                        await Task.Delay(100);
                    }
                }
                catch (HttpRequestException exp)
                {
                    break; // only no good proxies left
                }
                catch (Exception exp)
                {
                    // Something bad? maby no more proxies?
                }
            }
            // Download thread shutdown
            if (this.DownloaderThreadStatus != null)
            {
                this.DownloaderThreadStatus(this, threadIndex, false);
            }
        }

        private void HandleBadJob(IThreadedWebClientJob job, int threadIndex)
        {
            if (!this.badJobs.ContainsKey(job))
            {
                this.badJobs.GetOrAdd(job, 0);
            }
            this.badJobs[job]++;

            if (this.badJobs[job] < this.badJobRetry)
            {
                logger.Debug("Downloader {0} is adding job {1} back to the queue. {2} of {3} retires left", threadIndex, job, this.badJobs[job], this.badJobRetry - 1);
                this.Enqueue(job);
            }
            else
            {
                logger.Debug("Downloader {0}'s job {1} is out of retys.", threadIndex, job);
                // The jobs have failed multible times, inform and clean up
                int tryInt;
                this.badJobs.TryRemove(job, out tryInt);
                if (this.DownloaderBadJob != null)
                {
                    this.DownloaderBadJob(this, threadIndex, job);
                }
            }
        }

        /// <summary>
        /// Find a proxy for the WebClient to use
        /// </summary>
        /// <returns>Return true if continue, false if there are no proxies</returns>
        private WebProxy HandleAddProxy(out WebProxyHolder proxy)
        {
            // If any proxies exists, dequery the next in line and use it
            proxy = null;
            if (!this.useProxies) 
                return null;

            // No proxies avalible
            if (this.proxies.Count == 0)
            {
                if (this.NoGoodProxyEventFired)
                {
                    logger.Debug("No good proxies already fired.");
                    // No Good proxies, user will ensure it is handeled correctly.
                    return null;
                }

                // Bad no good proxies left
                logger.Debug("No good proxies found, and it is the first time.");
                this.NoGoodProxyEventFired = true;
                if (this.dieOnProxiesLeft)
                {
                    this.stopThread = true;
                }
                if (this.DownloaderNoGoodProxyLeft != null)
                {
                    this.DownloaderNoGoodProxyLeft(this);
                }
                // No Good proxies, user will ensure it is handeled correctly.
                return null;
            }

            WebProxy p = null;
            if (this.proxies.TryDequeue(out proxy))
            {
                logger.Trace("Found proxy {0}.", proxy);
                this.proxies.Enqueue(proxy);
                return new WebProxy(proxy.Ip, proxy.Port);
            }
            logger.Error("Was unable to dequeue a proxy.");
            // No Good proxies, user will ensure it is handeled correctly.
            return null;
        }

        /// <summary>
        /// Hanvlde bad proxy voting
        /// </summary>
        private void HandleBadProxy(int threadIndex, WebProxyHolder proxy)
        {
            // If we use proxies might the proxy be bad!
            // or it might be the url, we vote for bad proxies
            // to be removed.
            if (proxy != null && this.useProxies)
            {
                logger.Debug("Down voting the proxy {0}." , proxy);
                if (!this.badProxy.ContainsKey(proxy))
                {
                    logger.Debug("First time down voting proxy {0}.", proxy);
                    this.badProxy.GetOrAdd(proxy, 1);
                }
                else
                {
                    this.badProxy[proxy]++;
                    logger.Debug("Proxy {0} have failied {1} times now.", proxy, this.badProxy[proxy]);
                }

                if (this.badProxy[proxy] >= this.votesToRemoveProxy)
                {
                    logger.Debug("Proxy {0} have failied {1} or more times, removing it.", proxy, this.votesToRemoveProxy);
                    if (this.DownloaderBadProxyRemoved != null)
                    {
                        this.DownloaderBadProxyRemoved(this, threadIndex, proxy);
                    }
                    // Loop thoug all the proxies and requirie them again as long as they are not
                    // the bad proxie
                    WebProxyHolder current = null;
                    // To ensure the proxie have not been removed and we start a never ending loop
                    logger.Trace("Ensuring proxy {0} have been removed from the queue.", proxy);
                    int i = this.proxies.Count; 
                    while (current != proxy && i >= 0)
                    {
                        // Add the last current, as that is not the bad proxy
                        if (current != null)
                        {
                            this.proxies.Enqueue(current);
                        }
                        else
                        {
                            // The current proxy do not requque it
                        }
                        this.proxies.TryDequeue(out current);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Get the latest finished job, if there are no
        /// jobs avalible, will it return null.
        /// </summary>
        private IThreadedWebClientJob Dequeue()
        {
            IThreadedWebClientJob job;
            if (this.useDepthFirst)
            {
                if (this.JobsQueueStack.TryPop(out job))
                {
                    if (this.JobInQueueChanged != null)
                    {
                        int queue = this.useDepthFirst ? this.JobsQueueStack.Count : this.jobsQueue.Count;
                        this.JobInQueueChanged(this, queue);
                    }
                    return job;
                }
                return null;
            }
            if (this.jobsQueue.TryDequeue(out job))
            {
                if (this.JobInQueueChanged != null)
                {
                    int queue = this.useDepthFirst ? this.JobsQueueStack.Count : this.jobsQueue.Count;
                    this.JobInQueueChanged(this, queue);
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
                this.jobsQueue.Enqueue(job);
            }

            if (this.JobInQueueChanged != null)
            {
                int queue = this.useDepthFirst ? this.JobsQueueStack.Count : this.jobsQueue.Count;
                this.JobInQueueChanged(this, queue);
            }
        }
        
        public void ClearJobs()
        {
            if(this.useDepthFirst)
                this.JobsQueueStack = new ConcurrentStack<IThreadedWebClientJob>();
            else
                this.jobsQueue = new ConcurrentQueue<IThreadedWebClientJob>();
        }

        public bool IsRunning()
        {
            return !this.stopThread;
        }

        internal List<WebProxyHolder> GetProxies()
        {
            return this.proxies.ToList();
        }

        internal int GetCurrentProxyCount()
        {
            return this.proxies.Count;
        }
    }
}