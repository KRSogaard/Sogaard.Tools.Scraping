using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebClientProxyTesting
{
    public class ListWriter
    {
        public List<string> list { get; set; }

        public ListWriter(string path)
        {
            this.list = new List<string>();
            this.path = path;
        }

        public void Add(string line)
        {
            lock (this.list)
            {
                this.list.Add(line);
                this.Write();
            }
        }

        public void Write()
        {
            lock(this.list){
                File.WriteAllLines(this.path, this.list);
            }
        }

        public string path { get; set; }
    }
}
