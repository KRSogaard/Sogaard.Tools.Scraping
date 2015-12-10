using System.Collections.Concurrent;
using System.Collections.Generic;
using NLog;

namespace Sogaard.Tools.Scraping.Holders
{
    public abstract class HolderBase<T>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private ConcurrentQueue<T> queue;
        private int maxQueue;

        protected HolderBase(int maxQueue)
        {
            this.maxQueue = maxQueue;
            queue = new ConcurrentQueue<T>();
        } 

        public void Add(T obj)
        {
            Add(new List<T>() { obj });
        }

        public void Add(List<T> objs)
        {
            foreach (var obj in objs)
            {
                queue.Enqueue(obj);
            }
            if (queue.Count >= maxQueue)
            {
                WriteCurrentObjects();
            } 
        }

        public void WriteCurrentObjects()
        {
            List<T> writeItems = new List<T>();
            T item;
            while (!queue.IsEmpty && queue.TryDequeue(out item))
            {
                logger.Debug("Holder have {0} items, the limit is {1} saving to file.", queue.Count, maxQueue);
                writeItems.Add(item);
            }
            if(writeItems.Count > 0) { 
                Write(writeItems);
            }
        }

        public abstract void Collet();
        public abstract void Write(List<T> items);
    }
}
