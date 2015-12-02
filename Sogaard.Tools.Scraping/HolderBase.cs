using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogaard.Tools.Scraping
{
    public abstract class HolderBase<T>
    {
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
                writeItems.Add(item);
            }
            Write(writeItems);
        }

        public abstract void Write(List<T> items);
    }
}
