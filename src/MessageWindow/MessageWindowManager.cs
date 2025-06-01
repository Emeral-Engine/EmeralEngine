using EmeralEngine.Core;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmeralEngine.MessageWindow
{
    public class MessageWindowManager
    {
        public const string DEFAULT_FONT = "Yu Gothic UI";
        public const string FILENAME = "msw.json";
        private string file;
        public Dictionary<int, MessageWindowConfig> windows;

        public MessageWindowManager()
        {
            file = Path.Combine(MainWindow.pmanager.Temp.path, FILENAME);
            if (File.Exists(file))
            {
                windows = JsonSerializer.Deserialize<Dictionary<int, MessageWindowConfig>>(File.ReadAllText(file));
            }
            else
            {
                var msw_w = MainWindow.pmanager.Project.Size[0];
                var msw_h = MainWindow.pmanager.Project.Size[1] * 0.3;
                windows = new()
                {
                    [0] = new MessageWindowConfig()
                    {
                        Width = msw_w,
                        Height = (int)msw_h,
                        BgColor = "#FF3D3C3C",
                        Bg = "",
                        BgColorAlpha = 0.7,
                        BgAlpha = 0.5,
                        TextColor = "#ffffffff",
                        Interval = 100,
                        FontSize = 30,
                        Font = DEFAULT_FONT,
                        ScriptWidth = msw_w - 10,
                        WindowLeftPos = 0,
                        WindowBottom = 0,
                        ScriptLeftPos = 0,
                        ScriptTopPos = 0,
                        NamePlateWidth = (int)(msw_w * 0.2),
                        NamePlateHeight = (int)(msw_h * 0.3),
                        NamePlateBgColor = "#FFA9A9A9",
                        NamePlateBgColorAlpha = 0.7,
                        NamePlateBgImage = "",
                        NamePlateBgImageAlpha = 0.5,
                        NameFont = DEFAULT_FONT,
                        NameFontColor = "#FFFFFFFF",
                        NameFontSize = 24,
                        NamePlateLeftPos = 0,
                        NamePlateBottomPos = msw_h
                    }
                };
                File.WriteAllText(file, JsonSerializer.Serialize(windows));
            }
        }
        public void Dump(string to = "")
        {
            File.WriteAllText(to.Length > 0 ? to : file, JsonSerializer.Serialize(windows));
        }
        public void Add(MessageWindowConfig conf)
        {
            windows.Add(windows.Count, conf);
            Dump();
        }
        public void Replace(int num, MessageWindowConfig conf)
        {
            windows[num] = conf;
            Dump();
        }
    }
    class MessageWindowBuilder
    {
        MessageWindowConfig config;
        public MessageWindowBuilder(MessageWindowConfig conf)
        {
            config = conf;
        }
        public DockPanel Build()
        {
            var panel = new DockPanel();
            var grid = new Grid();
            var bgi = new Image()
            {
                Width = config.Width,
                Height = config.Height,
                Stretch = Stretch.Fill,
                Opacity = config.BgAlpha
            };
            if (!string.IsNullOrEmpty(config.Bg)) bgi.Source = Utils.CreateBmp(MainWindow.pmanager.GetResource(config.Bg));
            var bgcr = new System.Windows.Shapes.Rectangle()
            {
                Width = config.Width,
                Height = config.Height,
                Opacity = config.BgColorAlpha,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(config.BgColor)
            };
            var text = new Label()
            {
                Content = "サンプル",
                FontSize = config.FontSize,
                FontFamily = new FontFamily(config.Font),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(config.TextColor)
            };
            grid.Children.Add(bgi);
            grid.Children.Add(bgcr);
            grid.Children.Add(text);
            panel.Children.Add(grid);
            return panel;
        }
    }
    public class MessageWindowConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string BgColor { get; set; }
        public string Bg { get; set; }
        public double BgColorAlpha { get; set; }
        public double BgAlpha { get; set; }
        public string TextColor { get; set; }
        public int FontSize { get; set; }
        public string Font { get; set; }
        public double Interval { get; set; }
        public double WindowLeftPos {  get; set; }
        public double WindowBottom {  get; set; }
        public int ScriptWidth { get; set; }
        public double ScriptTopPos {  get; set; }
        public double ScriptLeftPos {  get; set; }
        public int NamePlateWidth {  get; set; }
        public int NamePlateHeight {  get; set; }
        public string NamePlateBgColor {  get; set; }
        public double NamePlateBgColorAlpha {  get; set; }
        public string NamePlateBgImage {  get; set; }
        public double NamePlateBgImageAlpha { get; set; }
        public string NameFont {  get; set; }
        public int NameFontSize {  get; set; }
        public string NameFontColor {  get; set; }
        public double NamePlateLeftPos {  get; set; }
        public double NamePlateBottomPos { get; set; }
    }
}
