using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping
{
    public static class ToolsHelper
    {
        public static string MakeXmlSafe(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c  != '\x0003')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
