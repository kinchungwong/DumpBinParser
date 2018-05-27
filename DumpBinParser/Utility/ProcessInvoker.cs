using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DumpBinParser.Utility
{
    public class ProcessInvoker : IDisposable
    {
        /// <summary>
        /// Path to executable
        /// </summary>
        public string ExePath
        {
            get;
            set;
        }

        public IList<string> InputText
        {
            get;
            set;
        } = new List<string>();

        public IList<string> OutputText
        {
            get;
            set;
        } = new List<string>();

        public IList<string> Arguments
        {
            get;
            set;
        } = new List<string>();

        public List<Exception> Exceptions
        {
            get;
        } = new List<Exception>();

        public Process Process
        {
            get;
            private set;
        }

        public Thread StreamReaderThread
        {
            get;
            private set;
        }

        public Thread StreamWriterThread
        {
            get;
            private set;
        }

        public void Run()
        {
            InvokeFunc();
        }

        public void Dispose()
        {
            if (Process != null)
            {
                Process.Dispose();
                Process = null;
            }
            if (StreamReaderThread != null &&
                StreamReaderThread.IsAlive)
            {
                StreamReaderThread.Join();
                StreamReaderThread = null;
            }
            if (StreamWriterThread != null &&
                StreamWriterThread.IsAlive)
            {
                StreamWriterThread.Join();
                StreamWriterThread = null;
            }
        }

        private void InvokeFunc()
        {
            try
            {
                Process = new Process();
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.FileName = ExePath;
                Process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                Process.StartInfo.Arguments = GetCombinedArgumentString();
                Process.StartInfo.RedirectStandardInput = true;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                StreamReaderThread = new Thread(new ThreadStart(StreamReaderThreadFunc));
                StreamWriterThread = new Thread(new ThreadStart(StreamWriterThreadFunc));
                Process.Start();
                StreamReaderThread.Start();
                StreamWriterThread.Start();
                Process.WaitForExit();
                StreamReaderThread.Join();
                StreamWriterThread.Join();
            }
            catch (Exception ex)
            {
                lock (Exceptions)
                {
                    Exceptions.Add(ex);
                }
            }
        }

        private void StreamReaderThreadFunc()
        {
            if (!Process.StartInfo.RedirectStandardOutput)
            {
                return;
            }
            var streamReader = Process.StandardOutput;
            if (streamReader == null)
            {
                return;
            }
            try
            {
                while (!streamReader.EndOfStream)
                {
                    string s = streamReader.ReadLine();
                    if (s == null)
                    {
                        return;
                    }
                    lock (OutputText)
                    {
                        OutputText.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (Exceptions)
                {
                    Exceptions.Add(ex);
                }
            }
        }

        private void StreamWriterThreadFunc()
        {
            if (!Process.StartInfo.RedirectStandardInput)
            {
                return;
            }
            var streamWriter = Process.StandardInput;
            if (streamWriter == null)
            {
                return;
            }
            int lineIndex = 0;
            try
            {
                while (true)
                {
                    string s = null;
                    lock (InputText)
                    {
                        if (lineIndex < InputText.Count)
                        {
                            s = InputText[lineIndex];
                            lineIndex++;
                        }
                    }
                    if (s == null)
                    {
                        return;
                    }
                    streamWriter.WriteLine(s);
                    streamWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                lock (Exceptions)
                {
                    Exceptions.Add(ex);
                }
            }
        }

        private string GetCombinedArgumentString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Arguments)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                if (s.IndexOf(' ') >= 0)
                {
                    sb.Append(EnsureQuoted(s));
                }
                else
                {
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }

        private static string EnsureQuoted(string s)
        {
            if (!s.StartsWith("\"") && !s.EndsWith("\""))
            {
                return ("\"" + s + "\"");
            }
            return s;
        }
    }
}
