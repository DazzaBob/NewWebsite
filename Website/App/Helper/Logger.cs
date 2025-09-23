using System.Text;
namespace Website.App.Helper
{
    public class Logger : IDisposable
    {
        private string[] LogLevelNames = new string[5];
        private string _filePathName;
        private long Counter;

        private System.Timers.Timer LazyWriterTimer;
        private int LazyWriterDelay;

        private object MutexPrimaryList;
        private object MutexCounter;
        private object MutexFilePath;
        private object MutexLazyWriter;

        private List<LogMessage> PrimaryList;
        public enum LogLevel : byte
        {
            Unknown = 0,
            Info = 1,
            Warn = 2,
            Error = 3,
            Debug = 4
        }
        private Logger()
        {
            LogLevelNames[(byte)LogLevel.Unknown] = "UNK";
            LogLevelNames[(byte)LogLevel.Info] = "INF";
            LogLevelNames[(byte)LogLevel.Warn] = "WRN";
            LogLevelNames[(byte)LogLevel.Error] = "ERR";
            LogLevelNames[(byte)LogLevel.Debug] = "DBG";

            LazyWriterDelay = 100;

            MutexPrimaryList = new object();
            MutexCounter = new object();
            MutexFilePath = new object();
            MutexLazyWriter = new object();
            _filePathName = string.Empty;
            PrimaryList = [];

            LazyWriterTimer = new System.Timers.Timer(); // { AutoReset = true };
        }
        public Logger(string pathName) : this()
        {
            if (string.IsNullOrEmpty(pathName))
                throw new ArgumentException("Path name cannot be null or empty.", nameof(pathName));

            if (Path.IsPathRooted(pathName))
                _filePathName = pathName;
            else
                throw new ArgumentException("Path must be absolute.", nameof(pathName));

            InsureLogFileExists(pathName);

            LazyWriterTimer.Interval = LazyWriterDelay;
            LazyWriterTimer.AutoReset = true;
            LazyWriterTimer.Enabled = true;
            LazyWriterTimer.SynchronizingObject = null; // No synchronization context   
            LazyWriterTimer.Elapsed += (sender, e) => LazyWriterTimerElapsed();
        }
        public void Add(string message, LogLevel level = LogLevel.Info)
        {
            lock (MutexPrimaryList)
            {
                lock (MutexCounter)
                {
                    if (Counter < 0) Counter = 0; // Prevent overflow

                    Counter++;

                    if (Counter > 9999999999) // Prevent overflow
                        Counter = 0;
                    PrimaryList.Clear();
                }
                string sMessage = "[" + LogLevelNames[(byte)level] + "] " + message;
                PrimaryList.Add(new LogMessage(sMessage, Counter));
            }
        }
        public void ChangeLogFile(string pathName)
        {
            LazyWriterTimer.Stop();
            if (PrimaryList.Count > 0)
            {
                WriteMessageToDisc();

                lock (MutexFilePath)
                {
                    _filePathName = pathName;
                }
                InsureLogFileExists(pathName);

                Thread.SpinWait(LazyWriterDelay);
            }
            LazyWriterTimer.Start();
        }
        public string FilePathName => _filePathName;
        private static bool InsureLogFileExists(string pathName)
        {
            if (!File.Exists(pathName))
            {
                try
                {
                    using (File.Create(pathName)) { }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        private static bool IsFileLocked(string fileName)
        {
            try
            {
                using (File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
        }
        private struct LogMessage
        {
            internal string Text { get; set; }
            internal long Sequence { get; set; }
            internal LogMessage(string text, long sequence)
            {
                Text = text;
                Sequence = sequence;
            }
        }
        private void WriteMessageToDisc()
        {
            string sMessage = GetMessage();
            if (string.IsNullOrEmpty(sMessage)) return;
            if (IsFileLocked(_filePathName)) return;

            using var writer = File.AppendText(_filePathName);
            writer.WriteLine(sMessage);
        }
        private string GetMessage()
        {
            var sb = new StringBuilder();
            lock (MutexPrimaryList)
            {
                PrimaryList.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
                foreach (var message in PrimaryList)
                {
                    string sText = message.Text;
                    string sTimeStamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                    sText = sTimeStamp + " " + sText;
                    if (!sText.EndsWith(Environment.NewLine))
                        sText += Environment.NewLine;
                    sb.Append(sText);
                }
                PrimaryList.Clear();
                lock (MutexCounter)
                {
                    Counter = 0;
                }
            }
            return sb.ToString();
        }
        private void LazyWriterTimerElapsed()
        {
            LazyWriterTimer.Stop();
            LazyWriterTimer.Enabled = false;

            int count = PrimaryList.Count;
            if (count > 1000)
                lock (MutexLazyWriter) { LazyWriterDelay = 49; }
            else if (count > 100)
                lock (MutexLazyWriter) { LazyWriterDelay = 99; }
            else if (count > 0)
                lock (MutexLazyWriter) { LazyWriterDelay = 259; }
            else
                lock (MutexLazyWriter) { LazyWriterDelay = 511; }

            Thread.SpinWait(LazyWriterDelay);

            if (PrimaryList.Count > 0)
                WriteMessageToDisc();
            else
                lock (MutexCounter) { Counter = 0; }

            LazyWriterTimer.Enabled = true;
            LazyWriterTimer.Start();
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    LazyWriterTimer?.Stop();
                    LazyWriterTimer?.Dispose();
                }
                if (PrimaryList.Count > 0)
                {
                    WriteMessageToDisc();
                    Thread.SpinWait(LazyWriterDelay);
                    Counter = 0;
                    LazyWriterDelay = 0;
                }
                PrimaryList?.Clear();
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