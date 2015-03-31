using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sogaard.Tools.Scraping.Multithreading;
using Sogaard.Tools.Scraping.Multithreading.GUI.Annotations;

namespace Sogaard.Scraper.HoursGuid.GUI.ViewModels
{
    public class ProxyViewModel : INotifyPropertyChanged
    {
        private WebProxyHolder proxy;
        private bool removed;

        public WebProxyHolder Proxy
        {
            get { return proxy; }
            set
            {
                proxy = value;
                OnPropertyChanged();
                OnPropertyChanged("GetDisplayText");
            }
        }
        public bool Remove
        {
            get { return removed; }
            set
            {
                removed = value;
                OnPropertyChanged();
                OnPropertyChanged("GetDisplayText");
            }
        }

        public string GetDisplayText
        {
            get
            {
                var ip = proxy.Ip + ":" + proxy.Port + " from " + proxy.Country;
                if (Remove)
                {
                    ip += " [Bad Proxy, Removed]";
                }
                return ip;
            }
        }

        public ProxyViewModel(WebProxyHolder holder)
        {
            Proxy = holder;
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