using System.IO;

namespace EmeralEngine.Core
{
    public class BaseInfo
    {
        private string _trans_color = "";
        public string trans_color
        {
            get => string.IsNullOrEmpty(_trans_color) ? "#000000" : _trans_color;
            set
            {
                _trans_color = value;
            }
        }
        public int trans { get; set; }
        public double interval { get; set; }
        public double fadein {  get; set; }
        public double fadeout {  get; set; }
        virtual public string Path {  get; set; }

        public void Remove()
        {
            File.Delete(Path);
        }
    }
}
