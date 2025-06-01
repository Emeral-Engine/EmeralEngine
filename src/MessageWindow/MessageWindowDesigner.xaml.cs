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
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

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
        private IEnumerator<string> script;
        public Border focusing_border;
        private WindowSettingsPage windowPage;
        private ScriptSettingPage scriptSettingPage;
        private NamePlateSettingPage namePlateSettingPage;
        private bool isNowPressing, isFromScript, isFromPlate;
        private bool isOnScript, isOnNamePlate;
        public int text_interval = 100; // ms
        private double ratio = 0.4;
        private double pre_x, pre_y = -1;
        public string bg = "";
        private int msw_num;
        private MouseInformation beginingMouseInfo;
        private double defaultWidth, defaultHeight;
        private MainWindow parent;
        public MessageWindowDesigner(MainWindow window)
        {
            InitializeComponent();
            parent = window;
            Owner = parent;
            defaultWidth = Width;
            defaultHeight = Height;
            script = GetSampleScript();
            windowPage = new(this);
            scriptSettingPage = new(this);
            namePlateSettingPage = new(this);
            ApplyMsw(parent.mmanager.windows[0]);
            WindowSample.Width = MainWindow.pmanager.Project.Size[0];
            WindowSample.Height = MainWindow.pmanager.Project.Size[1];
            WindowSampleScale.ScaleX = ratio;
            WindowSampleScale.ScaleY = ratio;
            var default_font = scriptSettingPage.FontList.FontFamily.ToString();
            var size = new FormattedText(
                SAMPLE_SCRIPT,
                CultureInfo.CurrentCulture,
                FlowDirection = FlowDirection.LeftToRight,
                new Typeface(default_font),
                Script.FontSize,
                Script.Foreground
                );
            ScriptBorder.Height = size.Height;
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
                            var size = new FormattedText(
                                SAMPLE_SCRIPT,
                                CultureInfo.CurrentCulture,
                                FlowDirection = FlowDirection.LeftToRight,
                                new Typeface(f.ToString()),
                                Script.FontSize,
                                Script.Foreground
                                );
                            ScriptBorder.Width = size.Width;
                            ScriptBorder.Height = size.Height;
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

        private void FocusUI(int ui)
        {
            if (focusing_border is not null)
            {
                focusing_border.BorderThickness = ZERO_THICK;
                if (focusing_border == ScriptBorder)
                {
                    ScriptBorder.Width -= BORDER_THICK.Left * 2;
                }
            }
            switch (ui)
            {
                case TargetUI.WINDOW:
                    focusing_border = MessageWindowBorder;
                    SettingFrame.Navigate(windowPage);
                    break;
                case TargetUI.SCRIPT:
                    focusing_border = ScriptBorder;
                    ScriptBorder.Width += BORDER_THICK.Left * 2;
                    SettingFrame.Navigate(scriptSettingPage);
                    break;
                case TargetUI.NAMEPLATE:
                    focusing_border = NamePlateBorder;
                    SettingFrame.Navigate(namePlateSettingPage);
                    break;
            }
            focusing_border.BorderThickness = BORDER_THICK;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScaleTrans.ScaleX = ActualWidth / defaultWidth;
            ScaleTrans.ScaleY = ActualHeight / defaultHeight;
        }

        private void OnMessageWindowMouseEnter(object sender, MouseEventArgs e)
        {
            if (focusing_border is null)
            {
                MessageWindowBorder.BorderThickness = BORDER_THICK;
            }

        }

        private void OnMessageWindowMouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
            if (!isNowPressing && focusing_border is null)
            {
                MessageWindowBorder.BorderThickness = ZERO_THICK;
            }
        }
        private MouseInformation ParseMousePos(MouseEventArgs e, int ui)
        {
            var border = ui switch
            {
                TargetUI.WINDOW => MessageWindowBorder,
                TargetUI.SCRIPT => ScriptBorder,
                _ => MessageWindowBorder
            };
            var pos = e.GetPosition(border);
            var width = border.BorderThickness.Left + ERROR_RANGE;
            var l = pos.X <= width;
            var r = border.ActualWidth - width <= pos.X;
            if (l || r)
            {
                return new MouseInformation
                {
                    isLeft = l,
                    isRight = r,
                    x = pos.X,
                    width = border.ActualWidth,
                    height = border.ActualHeight
                };
            }
            var t = pos.Y <= width;
            var b = border.ActualHeight - width <= pos.Y;
            if (b || t)
            {
                return new MouseInformation
                {
                    isBottom = b,
                    isTop = t,
                    y = pos.Y,
                    width = border.ActualWidth,
                    height = border.ActualHeight
                };
            }
            else return new MouseInformation
            {
            };
        }

        private void OnMessageWindowLeftDown(object sender, MouseButtonEventArgs e)
        {
            isNowPressing = true;
            Border border;
            Canvas canvas;
            if (isOnScript)
            {
                border = ScriptBorder;
                canvas = WindowContents;
                beginingMouseInfo = ParseMousePos(e, TargetUI.SCRIPT);
            }
            else
            {
                border = MessageWindowBorder;
                canvas = WindowSample;
                beginingMouseInfo = ParseMousePos(e, TargetUI.WINDOW);
            }
            var pos = e.GetPosition(canvas);
            if (beginingMouseInfo.isLeft)
            {
                border.ClearValue(Canvas.LeftProperty);
                Canvas.SetRight(border, Math.Max(canvas.ActualWidth - pos.X - border.ActualWidth, 0));
            }
            else if (beginingMouseInfo.isRight)
            {
                border.ClearValue(Canvas.RightProperty);
                Canvas.SetLeft(border, Math.Max(pos.X - border.ActualWidth, 0));
            }
            else if (beginingMouseInfo.isTop && !isOnScript)
            {
                border.ClearValue(Canvas.TopProperty);
                Canvas.SetBottom(border, Math.Max(canvas.ActualHeight - pos.Y - border.ActualHeight, 0));
            }
            else if (beginingMouseInfo.isBottom && !isOnScript)
            {
                border.ClearValue(Canvas.BottomProperty);
                Canvas.SetTop(border, Math.Max(pos.Y - border.ActualHeight, 0));
            }
            else
            {
                Cursor = Cursors.ScrollAll;
                if (border == MessageWindowBorder)
                {
                    if (border.ReadLocalValue(Canvas.LeftProperty) == DependencyProperty.UnsetValue)
                    {
                        var x = canvas.ActualWidth - Canvas.GetRight(border);
                        Canvas.SetLeft(border, Math.Max(x - border.ActualWidth, 0));
                        border.ClearValue(Canvas.RightProperty);
                    }
                    if (border.ReadLocalValue(Canvas.BottomProperty) == DependencyProperty.UnsetValue)
                    {
                        var y = canvas.ActualHeight - Canvas.GetTop(border);
                        Canvas.SetBottom(border, Math.Max(y - border.ActualHeight, 0));
                        border.ClearValue(Canvas.TopProperty);
                    }
                }
            }
            if (!isOnScript && !isOnNamePlate)
            {
                FocusUI(TargetUI.WINDOW);
            }
        }

        private void OnSampleWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            isNowPressing = false;
            isFromScript = false;
            isFromPlate = false;
            Cursor = Cursors.Arrow;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isNowPressing)
            {
                if (isFromScript)
                {
                    var pos = e.GetPosition(WindowContents);
                    if (pre_x is not double.NaN)
                    {
                        var l = Utils.GetLeftPos(WindowContents, ScriptBorder);
                        var t = Canvas.GetTop(ScriptBorder);
                        var x = l + pos.X - pre_x;
                        var y = t + pos.Y - pre_y;
                        if (l < 0 || t < 0)
                        {

                        }
                        else if (Cursor == Cursors.ScrollAll)
                        {
                            if (x + ScriptBorder.Width <= MessageWindowBorder.Width)
                            {
                                Canvas.SetLeft(ScriptBorder, Math.Max(x, 0));
                            }
                            else
                            {
                                Canvas.SetLeft(ScriptBorder, 0);
                            }
                            if (y + ScriptBorder.Height <= MessageWindowBorder.Height)
                            {
                                Canvas.SetTop(ScriptBorder, Math.Max(0, Math.Min(y, MessageWindowBorder.ActualHeight - ScriptBorder.ActualHeight)));
                            }
                            else
                            {
                                Canvas.SetTop(ScriptBorder, 0);
                            }

                        }
                        else if (Cursor == Cursors.SizeWE)
                        {
                            var w = ScriptBorder.Width + (beginingMouseInfo.isLeft ? pre_x - pos.X : pos.X - pre_x);
                            if (x + ScriptBorder.Width <= MessageWindowBorder.Width)
                            {
                                ScriptBorder.Width = Math.Max(w, 0);
                            }

                        }
                    }
                    pre_x = pos.X;
                    pre_y = pos.Y;
                }
                else if (isFromPlate)
                {
                    var pos = e.GetPosition(NamePlateBorder);
                    if (pre_x is not double.NaN)
                    {
                        var l = Canvas.GetLeft(NamePlateBorder);
                        var b = Canvas.GetBottom(NamePlateBorder);
                        var x = l + pos.X - pre_x;
                        var y = b + pre_y - pos.Y;
                        if (0 <= l && 0 <= b && x + NamePlateBorder.ActualWidth < WindowSample.ActualWidth && y + NamePlateBorder.ActualHeight < WindowSample.ActualHeight)
                        {
                            Canvas.SetLeft(NamePlateBorder, Math.Max(0, x));
                            Canvas.SetBottom(NamePlateBorder, Math.Max(0, y));
                        }
                    }
                    pre_x = pos.X;
                    pre_y = pos.Y;
                }
                else
                {
                    var pos = e.GetPosition(WindowSample);
                    if (pre_x is not double.NaN)
                    {
                        var l = Utils.GetLeftPos(WindowSample, MessageWindowBorder);
                        var x = l + pos.X - pre_x;
                        var b = Utils.GetBottomPos(WindowSample, MessageWindowBorder);
                        var y = b - pos.Y + pre_y;
                        if (l < 0 || b < 0 || x + MessageWindowBorder.ActualWidth > WindowSample.ActualWidth || y + MessageWindowBorder.ActualHeight > WindowSample.ActualHeight)
                        {

                        }
                        else if (Cursor == Cursors.ScrollAll)
                        {
                            Canvas.SetLeft(MessageWindowBorder, Math.Max(x, 0));
                            Canvas.SetBottom(MessageWindowBorder, Math.Max(y, 0));
                        }
                        else if (Cursor != Cursors.Arrow)
                        {
                            if (beginingMouseInfo.isLeft || beginingMouseInfo.isRight)
                            {
                                var w = MessageWindowBorder.Width + (beginingMouseInfo.isLeft ? pre_x - pos.X : pos.X - pre_x);
                                MessageWindowBorder.Width = Math.Max(w, 0);
                                windowPage.MessageWindowWidth.Text = MessageWindowBorder.Width.ToString();
                            }
                            else
                            {
                                var h = MessageWindowBorder.Height + (beginingMouseInfo.isTop ? pre_y - pos.Y : pos.Y - pre_y);
                                MessageWindowBorder.Height = Math.Max(h, 0);
                                windowPage.MessageWindowHeight.Text = MessageWindowBorder.Height.ToString();
                            }
                        }
                    }
                    pre_x = pos.X;
                    pre_y = pos.Y;
                }
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var r = ratio;
                ratio += 0 < e.Delta ? DELTA : -DELTA;
                if (ZOOM_MAX_LIMIT < ratio) ratio = ZOOM_MAX_LIMIT;
                else if (ratio < 0.4) ratio = 0.4;
                WindowSampleScale.ScaleX = ratio;
                WindowSampleScale.ScaleY = ratio;
                e.Handled = true;
            }
        }

        private void OnMessageWindowMouseMove(object sender, MouseEventArgs e)
        {
            if (focusing_border is null)
            {
                MessageWindowBorder.BorderThickness = BORDER_THICK;
            }

            if (isNowPressing || isOnNamePlate) return;
            var pos = ParseMousePos(e, TargetUI.WINDOW);
            if (pos.isLeft || pos.isRight) Cursor = Cursors.SizeWE;
            else if (pos.isTop || pos.isBottom) Cursor = Cursors.SizeNS;
            else
            {
                Cursor = Cursors.Arrow;
                return;
            }
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save(bool overwrite = false)
        {
            var conf = new MessageWindowConfig()
            {
                Width = (int)MessageWindowBorder.Width,
                Height = (int)MessageWindowBorder.Height,
                BgColor = MessageWindowBg.Fill.ToString(),
                Bg = bg,
                BgColorAlpha = windowPage.BgColorAlpha.Value,
                BgAlpha = windowPage.BgAlpha.Value,
                TextColor = Script.Foreground.ToString(),
                Interval = (int)scriptSettingPage.Interval.Value,
                FontSize = (int)scriptSettingPage.FontSize.Value,
                Font = scriptSettingPage.FontList.Text,
                ScriptWidth = (int)MessageWindowBorder.Width,
                NamePlateWidth = (int)NamePlateBorder.Width,
                NamePlateHeight = (int)NamePlateBorder.Height,
                NamePlateLeftPos = Canvas.GetLeft(NamePlateBorder),
                NamePlateBottomPos = Canvas.GetBottom(NamePlateBorder),
                NameFont = namePlateSettingPage.FontList.Text,
                NameFontSize = (int)namePlateSettingPage.FontSize.Value,
                NameFontColor = CharaName.Foreground.ToString(),
                NamePlateBgColor = NamePlateBg.Fill.ToString(),
                NamePlateBgColorAlpha = namePlateSettingPage.BgColorAlpha.Value,
                NamePlateBgImage = namePlateSettingPage.BgImage,
                NamePlateBgImageAlpha = namePlateSettingPage.BgImageAlpha.Value,
                WindowLeftPos = Utils.GetLeftPos(WindowSample, MessageWindowBorder),
                WindowBottom = Utils.GetBottomPos(WindowSample, MessageWindowBorder),
                ScriptLeftPos = Canvas.GetLeft(ScriptBorder),
                ScriptTopPos = Canvas.GetTop(ScriptBorder)
            };
            if (overwrite)
            {
                parent.mmanager.Replace(msw_num, conf);
                if (parent.now_scene.msw == msw_num)
                {
                    parent.LoadPreview(script: false, charas: false, bg: false);
                }
            }
            else
            {
                parent.mmanager.Add(conf);
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (isNowPressing && focusing_border is null)
            {
                isNowPressing = false;
                Cursor = Cursors.Arrow;
                MessageWindowBorder.BorderThickness = ZERO_THICK;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var w = new SelectMessageWindow(parent, parent.mmanager.windows);
            w.ShowDialog();
            if (w.Result is not null)
            {
                ApplyMsw(w.Result);
                msw_num = w.SelectedNum;
                OverWriteButton.IsEnabled = true;
            }
        }
        private void ApplyMsw(MessageWindowConfig conf)
        {
            windowPage.MessageWindowWidth.Value = conf.Width;
            windowPage.MessageWindowHeight.Value = conf.Height;
            MessageWindowBg.Fill = Utils.GetBrush(conf.BgColor);
            MessageWindowBg.Fill.Opacity = conf.BgColorAlpha;
            var b = conf.Bg;
            if (string.IsNullOrEmpty(b)) MessageWindowBgImage.Source = null;
            else
            {
                MessageWindowBgImage.Source = Utils.CreateBmp(MainWindow.pmanager.GetResource(b));
                windowPage.BgAlpha.IsEnabled = true;
                windowPage.BgAlphaText.IsEnabled = true;
            }
            windowPage.BgColorAlpha.Value = conf.BgColorAlpha;
            windowPage.AlphaText.Value = conf.BgColorAlpha;
            windowPage.BgAlpha.Value = conf.BgAlpha;
            windowPage.BgAlphaText.Value = conf.BgAlpha;
            windowPage.BgColor.SelectedColor = Utils.GetColor(conf.BgColor);
            Script.Foreground = Utils.GetBrush(conf.TextColor);
            scriptSettingPage.Interval.Text = conf.Interval.ToString();
            scriptSettingPage.FontSize.Text = conf.FontSize.ToString();
            Script.FontSize = conf.FontSize;
            scriptSettingPage.FontList.Text = conf.Font;
            var font = new FontFamily(conf.Font);
            scriptSettingPage.FontList.FontFamily = font;
            Script.FontFamily = font;
            var size = new FormattedText(
                SAMPLE_SCRIPT,
                CultureInfo.CurrentCulture,
                FlowDirection = FlowDirection.LeftToRight,
                new Typeface(conf.Font),
                Script.FontSize,
                Script.Foreground
                );
            ScriptBorder.Width = conf.ScriptWidth;
            ScriptBorder.Height = size.Height;
            MessageWindowBorder.Width = conf.Width;
            MessageWindowBorder.Height = conf.Height;
            NamePlateBorder.Width = conf.NamePlateWidth;
            NamePlateBorder.Height = conf.NamePlateHeight;
            NamePlateBg.Fill = Utils.GetBrush(conf.NamePlateBgColor);
            namePlateSettingPage.BgColor.SelectedColor = Utils.GetColor(conf.NamePlateBgColor);
            NamePlateBg.Opacity = conf.NamePlateBgColorAlpha;
            namePlateSettingPage.BgColorAlpha.Value = conf.NamePlateBgColorAlpha;
            namePlateSettingPage.BgColorAlphaText.Value = conf.NamePlateBgColorAlpha;
            if (string.IsNullOrEmpty(conf.NamePlateBgImage))
            {
                namePlateSettingPage.DeleteBgButton.IsEnabled = false;
                namePlateSettingPage.BgImageAlpha.IsEnabled = false;
                namePlateSettingPage.BgImageAlphaText.IsEnabled = false;

            }
            else
            {
                NamePlateBgImage.Source = Utils.CreateBmp(MainWindow.pmanager.GetResource(conf.NamePlateBgImage));
                namePlateSettingPage.DeleteBgButton.IsEnabled = true;
                namePlateSettingPage.BgImageAlpha.IsEnabled = true;
                namePlateSettingPage.BgImageAlphaText.IsEnabled = true;
            }
            NamePlateBgImage.Opacity = conf.NamePlateBgImageAlpha;
            namePlateSettingPage.BgImageAlpha.Value = conf.NamePlateBgImageAlpha;
            namePlateSettingPage.BgImageAlphaText.Value = conf.NamePlateBgImageAlpha;
            CharaName.Foreground = Utils.GetBrush(conf.NameFontColor);
            namePlateSettingPage.NameColor.SelectedColor = Utils.GetColor(conf.NameFontColor);
            CharaName.FontFamily = new FontFamily(conf.NameFont);
            namePlateSettingPage.FontList.Text = conf.NameFont;
            CharaName.FontSize = conf.NameFontSize;
            namePlateSettingPage.FontSize.Value = conf.NameFontSize;
            Canvas.SetLeft(MessageWindowBorder, conf.WindowLeftPos);
            Canvas.SetBottom(MessageWindowBorder, conf.WindowBottom);
            Canvas.SetLeft(ScriptBorder, conf.ScriptLeftPos);
            Canvas.SetTop(ScriptBorder, conf.ScriptTopPos);
            Canvas.SetLeft(NamePlateBorder, conf.NamePlateLeftPos);
            Canvas.SetBottom(NamePlateBorder, conf.NamePlateBottomPos);
            MessageWindowBorder.ClearValue(Canvas.RightProperty);
            MessageWindowBorder.ClearValue(Canvas.TopProperty);
            ScriptBorder.ClearValue(Canvas.RightProperty);
            ScriptBorder.ClearValue(Canvas.BottomProperty);
            NamePlateBorder.ClearValue(Canvas.RightProperty);
            NamePlateBorder.ClearValue(Canvas.TopProperty);
            SettingFrame.Navigate(windowPage);
        }

        private void OverWriteButton_Click(object sender, RoutedEventArgs e)
        {
            Save(true);
        }

        private void OnScriptMouseEnter(object sender, MouseEventArgs e)
        {
            isOnScript = true;
            if (focusing_border is null)
            {
                ScriptBorder.BorderThickness = BORDER_THICK;
                ScriptBorder.Width += BORDER_THICK.Left * 2;
                MessageWindowBorder.BorderThickness = ZERO_THICK;
            }
        }

        private void OnScriptMouseLeave(object sender, MouseEventArgs e)
        {
            isOnScript = false;
            if (!(isNowPressing && isFromScript) && focusing_border is null)
            {
                ScriptBorder.BorderThickness = ZERO_THICK;
                if (focusing_border is null) ScriptBorder.Width -= BORDER_THICK.Left * 2;
            }
        }

        private void OnScriptLeftDown(object sender, MouseButtonEventArgs e)
        {
            isFromScript = true;
            if (focusing_border != ScriptBorder)
            {
                FocusUI(TargetUI.SCRIPT);
            }
        }

        private void OnScriptLeftUp(object sender, MouseButtonEventArgs e)
        {
            isFromScript = false;
        }

        private void OnScriptMouseMove(object sender, MouseEventArgs e)
        {
            if (!isNowPressing)
            {
                var pos = ParseMousePos(e, TargetUI.SCRIPT);
                if (pos.isLeft || pos.isRight) Cursor = Cursors.SizeWE;
                else Cursor = Cursors.Arrow;
                if (focusing_border is null) ScriptBorder.BorderThickness = BORDER_THICK;
                e.Handled = true;
            }
        }

        private void OnWindowSampleLeftDown(object sender, MouseButtonEventArgs e)
        {
            pre_x = double.NaN;
            pre_y = double.NaN;
        }

        private void OnPlateMouseEnter(object sender, MouseEventArgs e)
        {
            isOnNamePlate = true;
            if (focusing_border is null)
            {
                NamePlateBorder.BorderThickness = BORDER_THICK;
                MessageWindowBorder.BorderThickness = ZERO_THICK;
            }
        }

        private void OnPlateMouseLeave(object sender, MouseEventArgs e)
        {
            isOnNamePlate = false;
            NamePlateBorder.BorderThickness = ZERO_THICK;
        }

        private void OnPlateMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void OnNamePlateLeftDown(object sender, MouseButtonEventArgs e)
        {
            Cursor = Cursors.ScrollAll;
            pre_x = double.NaN;
            pre_y = double.NaN;
            isNowPressing = true;
            isFromPlate = true;
            if (focusing_border != NamePlateBorder)
            {
                FocusUI(TargetUI.NAMEPLATE);
            }
            e.Handled = true;
        }

        private void OnNamePlateLeftUp(object sender, MouseButtonEventArgs e)
        {
            Cursor = Cursors.Arrow;
            isNowPressing = false;
            isFromScript = true;
        }
    }
    class MouseInformation
    {
        public bool isLeft = false;
        public bool isRight = false;
        public bool isTop = false;
        public bool isBottom = false;
        public double x, y, width, height;
    }
    struct TargetUI
    {
        public const int WINDOW = 1;
        public const int SCRIPT = 2;
        public const int NAMEPLATE = 3;
    }
}
