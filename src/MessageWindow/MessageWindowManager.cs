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
        public List<string> windows
        {
            get => Directory.GetFiles(MainWindow.pmanager.ProjectMswDir).ToList();
        }
        public MessageWindowData this[string f]
        {
            get => LoadWindow(Path.GetFileNameWithoutExtension(f) + ".xaml");
        }

        public MessageWindowManager()
        {
            if (!Directory.Exists(MainWindow.pmanager.ProjectMswDir))
            {
                var f = Path.Combine(MainWindow.pmanager.ProjectMswDir, "0.xaml");
                File.WriteAllText(f, MainWindow.pmanager.GetDefaultMsw());
            }
        }

        public IEnumerator<MessageWindowData> GetWindows()
        {
            foreach (var f in windows)
            {
                yield return LoadWindow(f);
            }
        }

        public MessageWindowData LoadWindow(string name)
        {
            return new MessageWindowData(XamlReader.Parse(XamlHelper.ConvertSourceToAbs(File.ReadAllText(Path.Combine(MainWindow.pmanager.ProjectMswDir, name)))) as FrameworkElement);
        }
    }

    public struct MessageWindowData
    {
        public Canvas WindowContents, NamePlate;
        public Image MessageWindowBg, NamePlateBg;
        public TextBlock Script;
        public Label CharaName;
        public MessageWindowData(FrameworkElement element)
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
        private const int PREVIEW_WIDTH = 300;
        private const int PREVIEW_HEIGHT = 100;
        private MessageWindowData window;
        public MessageWindowBuilder(MessageWindowData w)
        {
            window = w;
        }
        public DockPanel Build()
        {
            var panel = new DockPanel()
            {
                Width = PREVIEW_WIDTH,
                Height = PREVIEW_HEIGHT
            };
            var canvas = new Canvas();
            var mainwindow = new Canvas()
            {
                Width = window.WindowContents.Width,
                Height = window.WindowContents.Height,
                Background = window.WindowContents.Background,
            };
            mainwindow.Background.Opacity = window.WindowContents.Background.Opacity;
            canvas.Children.Add(mainwindow);
            var windowbg = new Image()
            {
                Width = window.WindowContents.Width,
                Height = window.WindowContents.Height,
                Opacity = window.MessageWindowBg.Opacity,
                Stretch = Stretch.Fill
            };
            if (window.MessageWindowBg.Source is not null)
            {
                windowbg.Source = Utils.CreateBmp(ImageUtils.GetFilePath(window.MessageWindowBg.Source));
            }
            canvas.Children.Add(windowbg);
            var script = new TextBlock()
            {
                Width = window.Script.Width,
                FontSize = window.Script.FontSize,
                FontFamily = window.Script.FontFamily,
                Foreground = window.Script.Foreground,
                Text = "サンプル"
            };
            Canvas.SetLeft(script, Canvas.GetLeft(window.Script) - Canvas.GetLeft(window.NamePlate));
            Canvas.SetTop(script, Canvas.GetTop(window.Script) - Canvas.GetTop(window.NamePlate));
            canvas.Children.Add(script);
            var nameplate = new Canvas()
            {
                Width = window.NamePlate.Width,
                Height = window.NamePlate.Height,
                Background = window.NamePlate.Background,
            };
            nameplate.Background.Opacity = window.NamePlate.Background.Opacity;
            nameplate.Visibility = Visibility.Visible;
            Canvas.SetLeft(nameplate, Canvas.GetLeft(window.NamePlate));
            Canvas.SetTop(nameplate, 0);
            canvas.Children.Add(nameplate);
            var nameplatebg = new Image()
            {
                Opacity = window.NamePlateBg.Opacity,
                Stretch = Stretch.Fill
            };
            if (window.NamePlateBg.Source is not null)
            {
                nameplatebg.Source = Utils.CreateBmp(ImageUtils.GetFilePath(window.NamePlateBg.Source));
            }
            canvas.Children.Add(nameplatebg);
            var speaker = new Label()
            {
                Foreground = window.CharaName.Foreground,
                FontFamily = window.CharaName.FontFamily,
                FontSize = window.CharaName.FontSize,
                Content = "名前"
            };
            canvas.Children.Add(speaker);
            var r = Math.Min(PREVIEW_WIDTH / window.WindowContents.Width, PREVIEW_HEIGHT / window.WindowContents.Height);
            canvas.LayoutTransform = new ScaleTransform()
            {
                ScaleX = r,
                ScaleY = r
            };
            panel.Children.Add(canvas);
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
