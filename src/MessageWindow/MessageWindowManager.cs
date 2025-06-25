using EmeralEngine.Core;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace EmeralEngine.MessageWindow
{
    public class MessageWindowManager
    {
        public const string DEFAULT_FONT = "Yu Gothic UI";
        public const string DIRNAME = "MessageWindows";
        public List<string> windows;
        public MessageWindow this[string f]
        {
            get => LoadWindow(Path.GetFileNameWithoutExtension(f) + ".xaml");
        }

        public MessageWindowManager()
        {
            if (Directory.Exists(MainWindow.pmanager.ProjectMswDir))
            {
                windows = Directory.GetFiles(MainWindow.pmanager.ProjectMswDir).ToList();
            }
            else
            {
                var f = Path.Combine(MainWindow.pmanager.ProjectMswDir, "0.xaml");
                File.WriteAllText(f, MainWindow.pmanager.GetDefaultMsw());
                windows = new() { f};
            }
        }

        public IEnumerator<MessageWindow> GetWindows()
        {
            foreach (var f in windows)
            {
                yield return LoadWindow(f);
            }
        }

        public MessageWindow LoadWindow(string name)
        {
            return new MessageWindow(XamlReader.Parse(XamlHelper.ConvertSourceToAbs(File.ReadAllText(Path.Combine(MainWindow.pmanager.ProjectMswDir, name)))) as FrameworkElement);
        }
    }

    public struct MessageWindow
    {
        public Canvas WindowContents, NamePlate;
        public Image MessageWindowBg, NamePlateBg;
        public TextBlock Script;
        public Label CharaName;
        public MessageWindow(FrameworkElement element)
        {
            WindowContents = element.FindName("WindowContents") as Canvas;
            MessageWindowBg = element.FindName("MessageWindowBgImage") as Image;
            Script = element.FindName("Script") as TextBlock;
            NamePlate = element.FindName("NamePlate") as Canvas;
            NamePlateBg = element.FindName("NamePlateBgImage") as Image;
            CharaName = element.FindName("CharaName") as Label;
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
