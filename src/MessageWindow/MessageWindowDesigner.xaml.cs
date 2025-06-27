using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using EmeralEngine.Resource;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
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
        private const int SCREEN_WIDTH = 500;
        private const int SCREEN_HEIGHT = 300;
        public const string SAMPLE_SCRIPT = "このようにテキストが表示されます\nこのようにテキストが表示されます\nこのようにテキストが表示されます";
        private IEnumerator<string> script;
        private DesignerElementManager elementManager;
        private WindowSettingsPage windowPage;
        private ScriptSettingPage scriptSettingPage;
        private NamePlateSettingPage namePlateSettingPage;
        public string Bg = "";
        private string CurrentMswPath;
        private double defaultWidth, defaultHeight;
        private MessageWindowManager MessageWindowManager;
        private MainWindow parent;
        public TextBlock Script;
        public Image NamePlateBgImage, MessageWindowBgImage;
        public Label CharaName;
        public Canvas NamePlate, WindowContents;
        private ResizableBorder NamePlateBorder, ScriptBorder, WindowBorder;
        public MessageWindowDesigner(MainWindow window)
        {
            InitializeComponent();
            parent = window;
            Owner = parent;
            MessageWindowManager = parent.Managers.MessageWindowManager;
            Loaded += (sender, e) =>
            {
                var r = Math.Min(SCREEN_WIDTH / Preview.Width, SCREEN_HEIGHT / Preview.Height);
                PreviewScale.ScaleX = r;
                PreviewScale.ScaleY = r;
            };
            defaultWidth = Width;
            defaultHeight = Height;
            script = GetSampleScript();
            elementManager = new();
            windowPage = new(this);
            scriptSettingPage = new(this);
            namePlateSettingPage = new(this);
            Load(MessageWindowManager.windows[0]);
            Preview.Width = parent.Managers.ProjectManager.Project.Size[0];
            Preview.Height = parent.Managers.ProjectManager.Project.Size[1];
            var default_font = scriptSettingPage.FontList.FontFamily.ToString();
            Task.Run(() =>
            {
                foreach (var f in Fonts.SystemFontFamilies)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        var item = new ComboBoxItem()
                        {
                            Content = f.ToString(),
                            FontFamily = f,
                            IsSelected = f.ToString() == default_font
                        };
                        item.Selected += (sender, e) =>
                        {
                            scriptSettingPage.FontList.FontFamily = f;
                            Script.FontFamily = f;
                        };
                        var item2 = new ComboBoxItem()
                        {
                            Content = f.ToString(),
                            FontFamily = f,
                            IsSelected = f.ToString() == default_font
                        };
                        item2.Selected += (sender, e) =>
                        {
                            namePlateSettingPage.FontList.FontFamily = f;
                            CharaName.FontFamily = f;
                        };
                        scriptSettingPage.FontList.Items.Add(item);
                        namePlateSettingPage.FontList.Items.Add(item2);
                    });
                }
            });

            Task.Run(ShowScript);
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScaleTrans.ScaleX = ActualWidth / defaultWidth;
            ScaleTrans.ScaleY = ActualHeight / defaultHeight;
        }

        private void OverWriteButton_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(CurrentMswPath, GenerateXaml());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var res = SelectMessageWindow.Select(this);
            if (0 <= res)
            {
                Load(MessageWindowManager.windows[res]);
            }
        }

        private string GenerateXaml()
        {
            var source = new StringBuilder();
            source.AppendLine("<Canvas xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">");
            string img;
            if (MessageWindowBgImage.Source is null)
            {
                img = "";
            }
            else
            {
                img = $"Source=\"{ImageUtils.GetFileName(MessageWindowBgImage.Source)}\"";
            }
            source.AppendLine($$"""
                <Canvas Name="WindowContents" Width="{{WindowContents.Width}}" Height="{{WindowContents.Height}}" Canvas.Top="{{Canvas.GetTop(WindowBorder)}}" Canvas.Left="{{Canvas.GetLeft(WindowBorder)}}">
                    <Canvas.Background>
                        <SolidColorBrush Color="{{Utils.GetHex(WindowContents.Background)}}" Opacity="{{WindowContents.Background.Opacity}}"/>
                    </Canvas.Background>
                    <Image Name="MessageWindowBgImage" Stretch="Fill" Height="{Binding ActualHeight, ElementName=WindowContents}" Width="{Binding ActualWidth, ElementName=WindowContents}" {{img}} Opacity="{{MessageWindowBgImage.Opacity}}"/>
                </Canvas>
                """);
            source.AppendLine($"""
                <TextBlock Name="Script" Width="{Script.Width}" TextAlignment="Left" HorizontalAlignment="Left" VerticalAlignment="Top"  Canvas.Left="{Canvas.GetLeft(ScriptBorder)}" Canvas.Top="{Canvas.GetTop(ScriptBorder)}" FontSize="{Script.FontSize}" Foreground="{Utils.GetHex(Script.Foreground)}" TextWrapping="WrapWithOverflow"/>
                """);
            string img2;
            if (NamePlateBgImage.Source is null)
            {
                img2 = "";
            }
            else
            {
                img2 = $"Source=\"{ImageUtils.GetFileName(NamePlateBgImage.Source)}\"";
            }
            source.AppendLine($$"""
                <Canvas Name="NamePlate" Width="{{NamePlate.Width}}" Height="{{NamePlate.Height}}" Canvas.Left="{{Canvas.GetLeft(NamePlateBorder)}}" Canvas.Top="{{Canvas.GetTop(NamePlateBorder)}}">
                    <Canvas.Background>
                        <SolidColorBrush Color="{{Utils.GetHex(NamePlate.Background)}}" Opacity="{{NamePlate.Background.Opacity}}"/>
                    </Canvas.Background>
                    <Image Name="NamePlateBgImage" Stretch="Fill" Height="{Binding ActualHeight, ElementName=NamePlate}" Width="{Binding ActualWidth, ElementName=NamePlate}" {{img2}} Opacity="{{NamePlateBgImage.Opacity}}"/>
                    <Label Name="CharaName" Content="名前" FontSize="{{CharaName.FontSize}}" Foreground="{{Utils.GetHex(CharaName.Foreground)}}" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Canvas>
                """);
            source.AppendLine("</Canvas>");
            return source.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(parent.Managers.ProjectManager.GetNextMswPath(), GenerateXaml());
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
                    await Task.Delay(MainWindow.pmanager.Project.TextInterval);
                }
                else
                {
                    script = GetSampleScript();
                    await Task.Delay(2000);
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Script.Text = "";
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
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
            CurrentMswPath = path;
            var canvas = (Canvas)XamlReader.Parse(XamlHelper.ConvertSourceToAbs(File.ReadAllText(path)));
            foreach (var element in canvas.Children.Cast<FrameworkElement>().ToArray())
            {
                switch (element.Name)
                {
                    case "NamePlate":
                        NamePlate = element as Canvas;
                        CharaName = (Label)NamePlate.FindName("CharaName");
                        NamePlateBgImage = (Image)NamePlate.FindName("NamePlateBgImage");
                        if (NamePlateBgImage.Source is not null)
                        {
                            NamePlateBgImage.Source = Utils.CreateBmp(ImageUtils.GetFilePath(NamePlateBgImage.Source)); // 排他ロック対策
                        }
                        canvas.Children.Remove(NamePlate);
                        NamePlateBorder = DragResizeHelper.Make(Preview, NamePlate);
                        Canvas.SetBottom(NamePlateBorder, Canvas.GetBottom(NamePlateBorder));
                        Canvas.SetLeft(NamePlateBorder, Canvas.GetLeft(NamePlateBorder));
                        Preview.Children.Add(NamePlateBorder);
                        namePlateSettingPage.FontList.Text = CharaName.FontFamily.ToString();
                        namePlateSettingPage.NameColor.SelectedColor = ((SolidColorBrush)CharaName.Foreground).Color;
                        namePlateSettingPage.FontSize.Value = (int)CharaName.FontSize;
                        namePlateSettingPage.BgColor.SelectedColor = ((SolidColorBrush)NamePlate.Background).Color;
                        namePlateSettingPage.BgColorAlpha.Value = NamePlate.Background.Opacity;
                        namePlateSettingPage.BgImageAlpha.Value = NamePlateBgImage.Opacity;
                        NamePlateBorder.PreviewMouseLeftButtonDown += (sender, e) =>
                        {
                            SettingFrame.Navigate(namePlateSettingPage);
                            elementManager.Focus(NamePlateBorder);
                        };
                        break;
                    case "Script":
                        Script = element as TextBlock;
                        canvas.Children.Remove(Script);
                        ScriptBorder = DragResizeHelper.Make(Preview, Script);
                        ScriptBorder.PreviewMouseLeftButtonDown += (sender, e) =>
                        {
                            SettingFrame.Navigate(scriptSettingPage);
                            elementManager.Focus(ScriptBorder);
                        };
                        ScriptBorder.Height = TextHelper.GetTextHeight(SAMPLE_SCRIPT, Script);
                        var fontDesc = DependencyPropertyDescriptor.FromProperty(TextBlock.FontFamilyProperty, typeof(TextBlock));
                        fontDesc.AddValueChanged(Script, (sender, e) =>
                        {
                            ScriptBorder.Height = TextHelper.GetTextHeight(SAMPLE_SCRIPT, Script);
                        });
                        var fontSizeDesc = DependencyPropertyDescriptor.FromProperty(TextBlock.FontSizeProperty, typeof(TextBlock));
                        fontSizeDesc.AddValueChanged(Script, (sender, e) =>
                        {
                            ScriptBorder.Height = TextHelper.GetTextHeight(SAMPLE_SCRIPT, Script);
                        });
                        scriptSettingPage.FontList.Text = Script.FontFamily.ToString();
                        scriptSettingPage.TextColorPicker.SelectedColor = ((SolidColorBrush)Script.Foreground).Color;
                        scriptSettingPage.FontSize.Value = (int)Script.FontSize;
                        Preview.Children.Add(ScriptBorder);
                        break;
                    case "WindowContents":
                        WindowContents = element as Canvas;
                        MessageWindowBgImage = (Image)WindowContents.FindName("MessageWindowBgImage");
                        if (MessageWindowBgImage.Source is not null)
                        {
                            MessageWindowBgImage.Source = Utils.CreateBmp(ImageUtils.GetFilePath(MessageWindowBgImage.Source));
                        }
                        canvas.Children.Remove(WindowContents);
                        WindowBorder = DragResizeHelper.Make(Preview, WindowContents);
                        WindowBorder.PreviewMouseLeftButtonDown += (sender, e) =>
                        {
                            SettingFrame.Navigate(windowPage);
                            elementManager.Focus(WindowBorder);
                        };
                        Preview.Children.Add(WindowBorder);
                        windowPage.BgColor.SelectedColor = ((SolidColorBrush)WindowContents.Background).Color;
                        windowPage.BgColorAlpha.Value = WindowContents.Background.Opacity;
                        windowPage.BgAlpha.Value = MessageWindowBgImage.Opacity;
                        windowPage.BgAlphaText.Value = MessageWindowBgImage.Opacity;
                        windowPage.MessageWindowWidth.Value = (int)WindowContents.Width;
                        windowPage.MessageWindowHeight.Value = (int)WindowContents.Height;
                        windowPage.BgAlpha.IsEnabled = MessageWindowBgImage.Source is not null;
                        windowPage.BgAlphaText.IsEnabled = windowPage.BgAlpha.IsEnabled;
                        break;
                    default:
                        canvas.Children.Remove(element);
                        Preview.Children.Add(DragResizeHelper.Make(Preview, element));
                        break;
                }
            }
            script = GetSampleScript();
        }
    }
}
