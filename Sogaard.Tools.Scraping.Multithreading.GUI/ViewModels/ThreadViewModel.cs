using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sogaard.Tools.Scraping.Multithreading.GUI.Annotations;

namespace Sogaard.Scraper.HoursGuid.GUI.ViewModels
{
    public class ThreadViewModel : INotifyPropertyChanged
    {
        private int threadId;
        private string status;
        private DateTime lastChange;

        public int ThreadId
        {
            get { return threadId; }
            set
            {
                threadId = value;
                OnPropertyChanged();
                OnPropertyChanged("GetDisplayText");
            }
        }
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                lastChange = DateTime.Now;
                OnPropertyChanged();
                OnPropertyChanged("GetDisplayText");
            }
        }

        public string GetDisplayText
        {
            get
            {
                string ret = "";
                ret += "[" + threadId.ToString("D2") + "]";
                ret += "[" + lastChange.Hour.ToString("D2") + ":" + lastChange.Minute.ToString("D2") + ":" + lastChange.Second.ToString("D2") + "]";
                ret += " " + status;
                return ret;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ThreadViewModel(int threadIndex)
        {
            this.ThreadId = threadIndex;
            Status = "Inactive";
            lastChange = DateTime.Now;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}