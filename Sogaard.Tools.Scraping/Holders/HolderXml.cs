using Sogaard.Tools.Scraping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using NLog;
using Sogaard.Tools.Scraping.Holders;

namespace UkData.Scraper.Holders
{
    public class HolderXml<T> : HolderBase<T>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string fileName;
        private int count = 1;

        public HolderXml(string fileName = "OutPut.{0}.xml", int maxQueue = 1000) : 
            base(maxQueue)
        {
            logger.Debug("Initalizing HolderXML with file path \"{0}\" and max queue {1} and of type \"{2}\"", fileName, maxQueue, typeof(T).Name);
            this.fileName = fileName;
        }

        public override void Write(List<T> items)
        {
            var name = string.Format(this.fileName, count);
            logger.Info("Saving {0} items to {1}", items.Count, name);
            try
            {
                var filePath = new FileInfo(name);
                if (!Directory.Exists(filePath.DirectoryName))
                {
                    logger.Info("Folder \"{0}\" did not exists, creating it.", filePath.DirectoryName);
                    Directory.CreateDirectory(filePath.DirectoryName);
                }
                XmlSerializer writer = new XmlSerializer(typeof(T[]));
                FileStream file = File.Create(name);
                writer.Serialize(file, items.ToArray());
                file.Close();

                count++;
            }
            catch (Exception exp)
            {
                logger.Error("Xml Serilazaion failed", exp);
            }
        }
    }
}
