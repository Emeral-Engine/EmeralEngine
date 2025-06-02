using System.IO;

namespace EmeralEngine.Core
{
    public class Logger
    {
        private const string FORMAT = "[{0}] ----------\r\n{1}\r\n----------------\r\n";
        private string file;
        public Logger(Managers m)
        {
            file = Path.Combine(m.ProjectManager.ActualProjectDir, ".log");
        }

        public void Write(string msg)
        {
            File.AppendAllText(file, string.Format(FORMAT, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg));
        }
    }
}
