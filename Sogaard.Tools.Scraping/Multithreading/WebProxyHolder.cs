namespace Sogaard.Tools.Scraping.Multithreading
{
    /// <summary>
    /// Holder class for proxy information
    /// </summary>
    public class WebProxyHolder
    {
        /// <summary>
        /// Proxy IP address
        /// </summary>
        public string Ip { get; set; }
        /// <summary>
        /// Proxy Port
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Proxy County, can be used for filtering
        /// </summary>
        public string Country { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Country))
            {
                return Ip + ":" + Port + " " + Country;
            }
            return Ip + ":" + Port;
        }
    }
}