using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Website.App.Helper
{
    public class Logger : IDisposable
    {
        private readonly string[] LogLevelNames = { "UNK", "INF", "WRN", "ERR", "DBG" };
        private string _filePathName;
        private long Counter;

        private readonly object MutexPrimaryList = new();
        private readonly object MutexFilePath = new();
        private readonly List<LogMessage> PrimaryList = [];

        private readonly System.Threading.Timer LazyWriterTimer;
        private int LazyWriterDelay = 100;
        private volatile bool _isFlushing = false;

        public enum LogLevel : byte { Unknown = 0, Info = 1, Warn = 2, Error = 3, Debug = 4 }
        public Logger(string pathName)
        {
            if (string.IsNullOrEmpty(pathName)) throw new ArgumentException("Path name cannot be null or empty.", nameof(pathName));
            if (!Path.IsPathRooted(pathName)) throw new ArgumentException("Path must be absolute.", nameof(pathName));

            _filePathName = pathName;
            InsureLogFileExists(pathName);

            // Initialize System.Threading.Timer
            LazyWriterTimer = new System.Threading.Timer(_ => LazyWriterTimerCallback(), null, LazyWriterDelay, LazyWriterDelay);
        }
        public void Add(string message, LogLevel level = LogLevel.Info)
        {
            string sMessage = $"[{LogLevelNames[(byte)level]}] {message}";
            lock (MutexPrimaryList)
            {
                Counter++;
                PrimaryList.Add(new LogMessage(sMessage, Counter));
            }
        }
        public void ChangeLogFile(string pathName)
        {
            Flush();
            lock (MutexFilePath)
            {
                _filePathName = pathName;
            }
            InsureLogFileExists(pathName);
        }
        public string FilePathName => _filePathName;
        private void LazyWriterTimerCallback()
        {
            if (_isFlushing) return;
            try
            {
                _isFlushing = true;

                List<LogMessage> snapshot;
                lock (MutexPrimaryList)
                {
                    if (PrimaryList.Count == 0) return;
                    snapshot = [.. PrimaryList]; // modern C# 12 clone
                    PrimaryList.Clear();
                }

                // Adaptive interval based on message count
                int count = snapshot.Count;
                int newInterval = count switch
                {
                    > 1000 => 50,
                    > 100 => 100,
                    > 0 => 250,
                    _ => 500
                };
                LazyWriterTimer.Change(newInterval, newInterval);

                // Build log text
                StringBuilder sb = new();
                snapshot.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
                foreach (var msg in snapshot)
                    sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg.Text}");

                // Write safely to disk
                lock (MutexFilePath)
                {
                    try
                    {
                        File.AppendAllText(_filePathName, sb.ToString(), Encoding.UTF8);
                    }
                    catch (IOException)
                    {
                        // Optional: could retry, skip, or queue for next flush
                    }
                }
            }
            finally
            {
                _isFlushing = false;
            }
        }
        public void Flush()
        {
            LazyWriterTimerCallback();
        }
        private static void InsureLogFileExists(string pathName)
        {
            if (!File.Exists(pathName))
            {
                using (File.Create(pathName)) { }
            }
        }
        private readonly struct LogMessage
        {
            internal string Text { get; }
            internal long Sequence { get; }
            internal LogMessage(string text, long sequence)
            {
                Text = text;
                Sequence = sequence;
            }
        }
        #region Implements IDisposable Pattern
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    LazyWriterTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    LazyWriterTimer?.Dispose();
                    Flush();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
