using System.IO;

namespace EmeralEngine.Core
{
    public class Logger
    {
        private const string FORMAT = "[{0}] [{1}]: {2}\r\n";
        private string file;
        public Logger(Managers m)
        {
            file = Path.Combine(m.ProjectManager.ActualProjectDir, "log.txt");
            Info("Start ----------");
        }

        public void Write(string msg, int level)
        {
            File.AppendAllText(file, string.Format(FORMAT, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), LoggingLevel.Get(level), msg));
        }

        public void Info(string msg)
        {
            Write(msg, LoggingLevel.INFO);
        }

        public void Warning(string msg)
        {
            Write(msg, LoggingLevel.WARNING);
        }

        public void Error(string msg)
        {
            Write(msg, LoggingLevel.ERROR);
        }
    }

    public struct LoggingLevel
    {
        public const int INFO = 1;
        public const int WARNING = 2;
        public const int ERROR = 3;
        public static Dictionary<int, string> _LoggingLevel = new() { {INFO, "Info" }, { WARNING, "Warning" }, { ERROR, "Error"} };

        public static string Get(int n)
        {
            return _LoggingLevel[n];
        }
    }
}
