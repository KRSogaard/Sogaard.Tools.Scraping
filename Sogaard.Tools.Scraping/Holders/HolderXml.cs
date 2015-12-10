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
using Sogaard.Tools.Scraping.Holders.Serilization;

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
            Write(items, name);
            count++;
        }

        private void Write(List<T> items, string name)
        {
            logger.Info("Saving {0} items to {1}", items.Count, name);
            try
            {
                var filePath = new FileInfo(name);
                if (!Directory.Exists(filePath.DirectoryName))
                {
                    logger.Info("Folder \"{0}\" did not exists, creating it.", filePath.DirectoryName);
                    Directory.CreateDirectory(filePath.DirectoryName);
                }
                XmlSerializer writer = getXmlSerializer();
                FileStream file = File.Create(name);
                writer.Serialize(file, items.ToArray());
                file.Close();
            }
            catch (Exception exp)
            {
                logger.Error("Xml Serilazaion failed", exp);
            }
        }

        public override void Collet()
        {
            SerializableList<T> allItems = new SerializableList<T>();
            int i = 1;
            FileInfo info = new FileInfo(string.Format(this.fileName, i));
            XmlSerializer writer = getXmlSerializer();
            try
            {
                while (info.Exists)
                {
                    logger.Debug("Loading file \"{0}\".", string.Format(this.fileName, i));
                    using (FileStream file = File.Open(info.FullName, FileMode.Open))
                    {
                        T[] list = (T[]) writer.Deserialize(file);
                        allItems.AddRange(list);
                    }

                i++;
                info = new FileInfo(string.Format(this.fileName, i));
                }
            }
            catch (Exception exp)
            {
                logger.Error(exp, "Loading xml serilized items failed. Leaveing the split files.");
                return;
            }

            logger.Info("{0} files was loaded from the split files.", allItems.Count);
            if (allItems.Count == 0)
            {
                return;
            }

            try
            {

                var name = string.Format(this.fileName, "all");
                logger.Info("Saving all {0} items to {1}", allItems.Count, name);
                Write(allItems, name);

                logger.Debug("Removing split files.");
                i = 1;
                name = string.Format(this.fileName, i);
                info = new FileInfo(name);
                while (info.Exists)
                {
                    try
                    {
                        logger.Trace("Removing split file \"{0}\".", name);
                        File.Delete(info.FullName);

                        i++;
                        name = string.Format(this.fileName, i);
                        info = new FileInfo(name);
                    }
                    catch (Exception exp)
                    {
                        logger.Error(exp, "Unable to remove split file \"{0}\".", name);
                    }
                }
            }
            catch (Exception exp)
            {
                logger.Error(exp, "Saving xml serilized items failed. Leaveing the split files.");
            }
        }

        private XmlSerializer getXmlSerializer()
        {
            return new XmlSerializer(typeof(T[]));
        }
    }
}
