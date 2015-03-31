using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WebClientProxyTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            ListWriter writer = new ListWriter("workingProxyList.txt");
            string url = "http://sogaardiptest.azurewebsites.net";
            List<Tuple<Tuple<string, int>, string>> workingProxyList = new List<Tuple<Tuple<string, int>, string>>();
            List<Tuple<Tuple<string, int>, string>> proxyList = new List<Tuple<Tuple<string, int>, string>>();
            foreach (var source in File.ReadAllLines("proxyList.txt").Where(x => !string.IsNullOrWhiteSpace(x)).ToList())
            {
                var split = source.Split(';');
                if (split.Length != 2)
                {
                    continue;
                }
                var ipSplit = split[0].Split(':');
                if (ipSplit.Length != 2)
                {
                    continue;
                }
                try
                {
                    proxyList.Add(new Tuple<Tuple<string, int>, string>(
                        new Tuple<string, int>(ipSplit[0], int.Parse(ipSplit[1])),
                        split[1]));
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exp: " + exp.Message);
                }
            }

            ConcurrentBag<Tuple<Tuple<string, int>, string>> _workingProxyList = new ConcurrentBag<Tuple<Tuple<string, int>, string>>();
            Parallel.ForEach(proxyList, (tuple, state) =>
            {

                using (WebClient client = new WebClient())
                {
                    var orginalIp = client.DownloadString(url).Trim();
                    Console.WriteLine("Testing: " + tuple);
                    try
                    {
                        client.Proxy = new WebProxy(tuple.Item1.Item1, tuple.Item1.Item2);
                        string ip = client.DownloadString(url).Trim();

                        IPAddress address;
                        if (IPAddress.TryParse(ip, out address))
                        {
                            if (ip != orginalIp)
                            {
                                _workingProxyList.Add(tuple);
                                writer.Add(tuple.Item1.Item1 + ":" + tuple.Item1.Item2 + ";" + tuple.Item2);
                            }
                            else
                            {
                                Console.WriteLine("Returned orginal ip");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Returned spam");
                        }
                    }
                    catch (WebException exp)
                    {
                        Console.WriteLine(exp.Message);
                    }
                }
            });
        }
    }
}
