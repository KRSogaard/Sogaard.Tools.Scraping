using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using NLog;

namespace Sogaard.Tools.Scraping.Holders
{
    public class HolderJson<T> : HolderBase<T>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string fileName;
        private int count = 1;

        public HolderJson(string fileName = "OutPut.{0}.json", int maxQueue = 1000) : 
            base(maxQueue)
        {
            logger.Debug("Initalizing HolderJSON with file path \"{0}\" and max queue {1} and of type \"{2}\"", fileName, maxQueue, typeof(T).Name);
            this.fileName = fileName;
        }

        public override void Write(List<T> items)
        {
            var name = string.Format(this.fileName, count);
            logger.Info("Saving {0} items to {1}", items.Count, name);

            var filePath = new FileInfo(name);
            if (!Directory.Exists(filePath.DirectoryName))
            {
                logger.Info("Folder \"{0}\" did not exists, creating it.", filePath.DirectoryName);
                Directory.CreateDirectory(filePath.DirectoryName);
            }

            var json = new JavaScriptSerializer().Serialize(items);
            File.WriteAllText(name, json);
            count++;
        }
    }
}
