using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using EmeralEngine.Resource;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace EmeralEngine
{
    /// <summary>
    /// MessageDesignerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MessageWindowDesigner : Window
    {
        private const double DELTA = 0.4;
        private const double ZOOM_MAX_LIMIT = 0.8;
        private int ERROR_RANGE = 5;
        public const string SAMPLE_SCRIPT = "このようにテキストが表示されます\nこのようにテキストが表示されます\nこのようにテキストが表示されます";
        private const int MIN_MESSAGEWINDOW_SIZE = 10;
        public Thickness BORDER_THICK = new Thickness(3);
        private Thickness ZERO_THICK = new Thickness(0);
        private string[] xamls;
        private IEnumerator<string> script;
        public Border focusing_border;
        private WindowSettingsPage windowPage;
        private ScriptSettingPage scriptSettingPage;
        private NamePlateSettingPage namePlateSettingPage;
        private bool isNowPressing, isFromScript, isFromPlate;
        private bool isOnScript, isOnNamePlate;
        public int text_interval = 100; // ms
        private double ratio = 0.4;
        public string bg = "";
        private string CurrentMsw;
        private double defaultWidth, defaultHeight;
        private MainWindow parent;
        public TextBlock Script;
        public Image NamePlateBgImage, MessageWindowBgImage;
        public Label CharaName;
        public Canvas NamePlate, WindowContents;
        public MessageWindowDesigner(MainWindow window)
        {
            InitializeComponent();
            parent = window;
            Owner = parent;
            Loaded += (sender, e) =>
            {
                var r = Math.Min(Preview.ActualWidth / Preview.Width, Preview.ActualHeight / Preview.Height);
                Debug.WriteLine(r);
                PreviewScale.ScaleX = r;
                PreviewScale.ScaleY = r;
            };
            defaultWidth = Width;
            defaultHeight = Height;
            script = GetSampleScript();
            windowPage = new(this);
            scriptSettingPage = new(this);
            namePlateSettingPage = new(this);
            xamls = parent.Managers.ProjectManager.GetMessageWindows();
            if (xamls.Length == 0)
            {
                var f = parent.Managers.ProjectManager.GetNextMswPath();
                File.WriteAllText(f, parent.Managers.ProjectManager.GetDefaultMsw());
                xamls = xamls.Append(f).ToArray();
            }
            CurrentMsw = xamls[0];
            Load(CurrentMsw);
            Preview.Width = parent.Managers.ProjectManager.Project.Size[0];
            Preview.Height = parent.Managers.ProjectManager.Project.Size[1];
            Task.Run(ShowScript);
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScaleTrans.ScaleX = ActualWidth / defaultWidth;
            ScaleTrans.ScaleY = ActualHeight / defaultHeight;
        }

        private async void ShowScript()
        {
            var f = true;
            while (f)
            {
                if (script.MoveNext())
                {
                    Dispatcher.Invoke(() =>
                    {
                        Script.Text += script.Current;
                    });
                    await Task.Delay(text_interval);
                }
                else
                {
                    script = GetSampleScript();
                    await Task.Delay(2000);
                    Dispatcher.Invoke(() =>
                    {
                        Script.Text = "";
                    });
                }
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        f = IsLoaded;
                    });
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
        private static IEnumerator<string> GetSampleScript()
        {
            foreach (var s in SAMPLE_SCRIPT)
            {
                yield return s.ToString();
            }
        }

        private void Load(string path)
        {
            Preview.Children.Clear();
            var canvas = (Canvas)XamlReader.Parse(parent.Managers.ProjectManager.ReadMswXaml(path));
            foreach (var element in canvas.Children.Cast<FrameworkElement>().ToArray())
            {
                switch (element.Name)
                {
                    case "NamePlate":
                        NamePlate = element as Canvas;
                        CharaName = (Label)NamePlate.FindName("CharaName");
                        NamePlateBgImage = (Image)NamePlate.FindName("NamePlateBgImage");
                        canvas.Children.Remove(NamePlate);
                        var n = DragResizeHelper.Make(Preview, NamePlate);
                        Canvas.SetBottom(n, Canvas.GetBottom(n));
                        Canvas.SetLeft(n, Canvas.GetLeft(n));
                        Preview.Children.Add(n);
                        namePlateSettingPage.FontList.Text = CharaName.FontFamily.ToString();
                        namePlateSettingPage.NameColor.SelectedColor = ((SolidColorBrush)CharaName.Foreground).Color;
                        namePlateSettingPage.FontSize.Value = (int)CharaName.FontSize;
                        namePlateSettingPage.BgColor.SelectedColor = ((SolidColorBrush)NamePlate.Background).Color;
                        namePlateSettingPage.BgColorAlpha.Value = NamePlate.Background.Opacity;
                        namePlateSettingPage.BgImageAlpha.Value = NamePlateBgImage.Opacity;
                        break;
                    case "WindowContents":
                        WindowContents = element as Canvas;
                        Script = (TextBlock)WindowContents.FindName("Script");
                        MessageWindowBgImage = (Image)WindowContents.FindName("MessageWindowBgImage");
                        WindowContents.Children.Remove(Script);
                        WindowContents.Children.Add(DragResizeHelper.Make(Preview, Script));
                        scriptSettingPage.FontList.Text = Script.FontFamily.ToString();
                        scriptSettingPage.TextColorPicker.SelectedColor = ((SolidColorBrush)Script.Foreground).Color;
                        scriptSettingPage.FontSize.Value = (int)Script.FontSize;
                        canvas.Children.Remove(WindowContents);
                        Preview.Children.Add(DragResizeHelper.Make(Preview, WindowContents));
                        windowPage.BgColor.SelectedColor = ((SolidColorBrush)WindowContents.Background).Color;
                        windowPage.BgColorAlpha.Value = WindowContents.Background.Opacity;
                        windowPage.BgAlpha.Value = MessageWindowBgImage.Opacity;
                        windowPage.MessageWindowWidth.Value = (int)WindowContents.Width;
                        windowPage.MessageWindowHeight.Value = (int)WindowContents.Height;
                        break;
                    default:
                        canvas.Children.Remove(element);
                        Preview.Children.Add(DragResizeHelper.Make(Preview, element));
                        break;
                }
            }
        }
    }
}
