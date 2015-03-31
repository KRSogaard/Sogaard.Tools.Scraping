using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Sogaard.Tools.Scraping.Multithreading;
using Sogaard.Tools.Scraping.Multithreading.GUI.Annotations;

namespace Sogaard.Scraper.HoursGuid.GUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public DateTime Started;

        private int downloadThreadCount;
        private int workThreadCount;
        private int downloadJobsInQueue;
        private int awaitingDownDownloadJobs;
        private int downloadJobsCurrentlyProcessing;
        private int workJobsInQueue;
        private int completedJobs;
        private int totalProxyCount;
        private int currentProxyCount;
        private bool _canStart;
        private bool _canStop;
        private bool _canResume;
        private bool _isPaused;
        private bool _isRunning;
        private ObservableCollection<ThreadViewModel> workThreads;
        private ObservableCollection<ThreadViewModel> downloadThreads;
        private ObservableCollection<ProxyViewModel> proxies;

        public int DownloadThreadCount
        {
            get { return downloadThreadCount; }
            set
            {
                downloadThreadCount = value;
                OnPropertyChanged();
            }
        }
        public int WorkThreadCount
        {
            get { return workThreadCount; }
            set
            {
                workThreadCount = value;
                OnPropertyChanged();
            }
        }
        public int DownloadJobsInQueue
        {
            get { return downloadJobsInQueue; }
            set
            {
                downloadJobsInQueue = value;
                OnPropertyChanged();
                OnPropertyChanged("TimeElapsed");
                OnPropertyChanged("TimeToComplete");
            }
        }
        public int AwaitingDownDownloadJobs
        {
            get { return awaitingDownDownloadJobs; }
            set
            {
                awaitingDownDownloadJobs = value;
                OnPropertyChanged();
                OnPropertyChanged("TimeElapsed");
                OnPropertyChanged("TimeToComplete");
            }
        }
        public int DownloadJobsCurrentlyProcessing
        {
            get { return downloadJobsCurrentlyProcessing; }
            set
            {
                downloadJobsCurrentlyProcessing = value;
                OnPropertyChanged();
                OnPropertyChanged("TimeElapsed");
                OnPropertyChanged("TimeToComplete");
            }
        }
        public int WorkJobsInQueue
        {
            get { return workJobsInQueue; }
            set
            {
                workJobsInQueue = value;
                OnPropertyChanged();
                OnPropertyChanged("TimeElapsed");
                OnPropertyChanged("TimeToComplete");
            }
        }
        public int CompletedJobs
        {
            get { return completedJobs; }
            set
            {
                completedJobs = value;
                OnPropertyChanged();
                OnPropertyChanged("TimeElapsed");
                OnPropertyChanged("JobsPerSecond");
                OnPropertyChanged("TimeToComplete");
            }
        }
        public int TotalProxyCount
        {
            get { return totalProxyCount; }
            set
            {
                totalProxyCount = value;
                OnPropertyChanged();
            }
        }
        public int CurrentProxyCount
        {
            get { return currentProxyCount; }
            set
            {
                currentProxyCount = value;
                OnPropertyChanged();
            }
        }

        public string TimeElapsed
        {
            get { return (DateTime.Now - Started).ToString("g"); }
        }
        public string JobsPerSecond
        {
            get { return (completedJobs/(DateTime.Now - Started).TotalSeconds).ToString("N"); }
        }
        public string TimeToComplete
        {
            get
            {
                var secs = ((double)this.DownloadJobsInQueue + (double)this.awaitingDownDownloadJobs + (double)this.WorkJobsInQueue) / (completedJobs / (DateTime.Now - Started).TotalSeconds);
                var span = new TimeSpan(0, 0, (int)secs);
                string output = " ";
                if(span.Hours > 1){
                    output += span.Hours + "h ";
                }
                if(span.Minutes > 1){
                    output += span.Minutes + "m ";
                }
                output += span.Seconds + "s ";
                return output.Trim();
            }
        }

        public ObservableCollection<ThreadViewModel> WorkThreads
        {
            get { return workThreads; }
            set
            {
                workThreads = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ThreadViewModel> DownloadThreads
        {
            get { return downloadThreads; }
            set
            {
                downloadThreads = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ProxyViewModel> Proxies
        {
            get { return proxies; }
            set
            {
                proxies = value;
                OnPropertyChanged();
            }
        }

        public bool CanStart
        {
            get { return _canStart; }
            set
            {
                _canStart = value;
                OnPropertyChanged();
                OnPropertyChanged("CanChangeThreads");
            }
        }
        public bool CanStop
        {
            get { return _canStop; }
            set
            {
                _canStop = value;
                OnPropertyChanged();
                OnPropertyChanged("CanChangeThreads");
            }
        }
        public bool CanResume
        {
            get { return _canResume; }
            set
            {
                _canResume = value;
                OnPropertyChanged();
            }
        }

        public bool CanChangeThreads
        {
            get { return _canStart; }
        }

        public string ResumeButtonText
        {
            get
            {
                if (!_isPaused)
                    return "Pause";
                else
                    return "Resume";
            }
        }

        public Visibility ShowProgressBar
        {
            get
            {
                if(_isRunning)
                    return Visibility.Visible;
                return Visibility.Hidden;
            }
        }

        public MainViewModel()
        {
            Started = DateTime.Now;
            DownloadThreads = new ObservableCollection<ThreadViewModel>();
            WorkThreads = new ObservableCollection<ThreadViewModel>();
            Proxies = new ObservableCollection<ProxyViewModel>();

            Proxies.CollectionChanged += (sender, args) =>
            {
                TotalProxyCount = Proxies.Count;
                CurrentProxyCount = Proxies.Count(x => !x.Remove);
            };
        }

        public void SetWorkStatus(int threadId, string status)
        {
            if (status.Contains("Object"))
            {
                status = "Inactive";
            }
            if (threadId < WorkThreads.Count)
            {
                WorkThreads[threadId].Status = status;
            }
        }
        public void SetDownloadStatus(int threadId, string status)
        {
            if (threadId < DownloadThreads.Count)
            {
                DownloadThreads[threadId].Status = status;
            }
        }
        public void SetProxyAsRemoved(WebProxyHolder holder)
        {
            foreach (var proxyViewModel in Proxies.Where(proxyViewModel => proxyViewModel.Proxy == holder))
            {
                proxyViewModel.Remove = true;
                break;
            }
            CurrentProxyCount--;
        }
        public void OnStart()
        {
            Started = DateTime.Now;
            _isRunning = true;
            CanStart = false;
            CanStop = true;
            CanResume = true;
            _isPaused = false;
            OnPropertyChanged("CanStart");
            OnPropertyChanged("CanStop");
            OnPropertyChanged("CanResume");
            OnPropertyChanged("ResumeButtonText");
            OnPropertyChanged("ShowProgressBar");
        }
        public void OnStop()
        {
            _isRunning = false;
            CanStart = true;
            CanStop = false;
            CanResume = false;
            _isPaused = false;
            OnPropertyChanged("CanStart");
            OnPropertyChanged("CanStop");
            OnPropertyChanged("CanResume");
            OnPropertyChanged("ResumeButtonText");
            OnPropertyChanged("ShowProgressBar");
        }
        public void OnPause()
        {
            _isRunning = false;
            _isPaused = true;
            OnPropertyChanged("ResumeButtonText");
            OnPropertyChanged("ShowProgressBar");
        }
        public void OnResume()
        {
            _isRunning = true;
            _isPaused = false;
            OnPropertyChanged("ResumeButtonText");
            OnPropertyChanged("ShowProgressBar");
        }

        public void ClearThreads()
        {
            WorkThreads.Clear();
            DownloadThreads.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
