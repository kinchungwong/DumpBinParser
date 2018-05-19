using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace DumpBinParser.Utility
{
    internal class StreamReaderThread : IDisposable
    {
        internal StreamReader Source { get; }
        internal ConcurrentQueue<string> Lines { get; }
        internal ConcurrentQueue<Exception> Exceptions { get; }
        private Thread Thread { get; set; }

        internal StreamReaderThread(StreamReader source)
        {
            Source = source;
            Lines = new ConcurrentQueue<string>();
            Exceptions = new ConcurrentQueue<Exception>();
            Thread = new Thread(new ThreadStart(ThreadFunc));
            Thread.Start();
        }

        private void ThreadFunc()
        {
            try
            {
                while (!Source.EndOfStream)
                {
                    string s = Source.ReadLine();
                    if (s == null)
                    {
                        return;
                    }
                    Lines.Enqueue(s);
                }
            }
            catch (Exception ex)
            {
                Exceptions.Enqueue(ex);
            }
        }

        public void Dispose()
        {
            WaitForExit();
        }

        public void WaitForExit()
        {
            if (Thread != null && Thread.IsAlive)
            {
                Thread.Join();
            }
        }
    }
}
