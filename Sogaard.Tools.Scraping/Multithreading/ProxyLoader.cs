using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Sogaard.Tools.Scraping.Multithreading
{
    public class ProxyLoader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DirectoryInfo directory { get; set; }
        private int minimumProxies { get; set; }
        private TimeSpan ScanInterval { get; set; }
        private ThreadedWebClientDownloader downloader { get; set; }
        private bool executeScan { get; set; }
        
        public ProxyLoader(ThreadedWebClientDownloader downloader, DirectoryInfo directory, int minimumProxies = 10, TimeSpan? scanInterval = null)
        {
            this.ScanInterval = scanInterval ?? new TimeSpan(0, 1, 0);
            this.executeScan = true;
            this.minimumProxies = minimumProxies;
            this.downloader = downloader;
            this.directory = directory;
            downloader.DownloaderBadProxyRemoved += DownloaderOnDownloaderBadProxyRemoved;
            
            logger.Debug("Initalizing ProxyLoader with:\nDirectory: \"{0}\"\nMinimumProxies: {1}\nScanInterval: {2}", directory.FullName, minimumProxies, ScanInterval);
        }

        private void DownloaderOnDownloaderBadProxyRemoved(object sender, int threadId, WebProxyHolder proxy)
        {
            CheckIfToStopProxies();
        }

        private void CheckIfToStopProxies()
        {
            if (this.downloader.GetCurrentProxyCount() <= 10)
            {
                logger.Warn("Download have {0} proxies left {1} is minimum, stopping all download jobs untill more proxies are present. This might get called multible times.", this.downloader.GetCurrentProxyCount(), this.minimumProxies);
                this.downloader.Stop();
            }
        }

        public void Run()
        {
            this.executeScan = true;
            Task.Factory.StartNew(() =>
            {
                RunAsync();
            });
        }

        public void Stop()
        {
            this.executeScan = false;
        }

        public void RunAsync()
        {
            logger.Debug("Checking \"{0}\" for new proxy files.", directory.FullName);
            if(!executeScan)
                return;

            var files = directory.GetFiles();
            logger.Debug("Found {0} files.", files.Length);
            foreach (var file in files)
            {
                try
                {
                    logger.Info("Loading proxy file \"{0}\".", file.Name);
                    var lines = File.ReadAllLines(file.FullName);
                    int added = 0;
                    foreach (var line in lines)
                    {
                        try
                        {
                            var split = line.Split(new []{ ',', ':'});
                            var proxyHolder = new WebProxyHolder();
                            proxyHolder.Ip = split[0];
                            proxyHolder.Port = int.Parse(split[1]);
                            if (split.Length >= 3)
                            {
                                proxyHolder.Country = split[2];
                            }
                            this.downloader.AddProxies(new List<WebProxyHolder>() {proxyHolder});
                            added++;
                        }
                        catch (Exception exp)
                        {
                            logger.Error(exp, "Failed to load proxy line \"{0}\".", line);
                        }
                    }
                    if (added > 0)
                    {
                        logger.Debug("Deleting \"{0}\" the {1} proxies have been loaded.", file.Name, added);
                        File.Delete(file.FullName);
                    }
                    else
                    {
                        logger.Debug("Did not find any proxies in \"{0}\".", file.Name);
                    }
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "Failed to load proxies from \"{0}\".", file.Name);
                }
            }

            if (!this.downloader.IsRunning() && this.downloader.GetCurrentProxyCount() > this.minimumProxies)
            {
                logger.Info("Downloader is stopped, and enough proxies have been added to restart the downloader.");
                this.downloader.Start();
            }

            Thread.Sleep(this.ScanInterval);
            RunAsync();
        }

    }
}
