using EmeralEngine.Core;
using EmeralEngine.Resource;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmeralEngine.MessageWindow
{
    /// <summary>
    /// WindowSettingsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowSettingsPage : Page
    {
        private MessageWindowDesigner window;
        public WindowSettingsPage(MessageWindowDesigner w)
        {
            InitializeComponent();
            window = w;
        }

        private void OnBgColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (IsLoaded)
            {
                window.WindowContents.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(BgColor.SelectedColorText);
            }
        }
        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                BgColorAlphaText.Text = Utils.CutString(BgColorAlpha.Value.ToString(), 4, false);
                window.WindowContents.Background.Opacity = BgColorAlpha.Value;
            }
        }
        private void OnBgSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                BgAlphaText.Text = Utils.CutString(BgAlpha.Value.ToString(), 4, false);
                window.MessageWindowBgImage.Opacity = BgAlpha.Value;
            }
        }

        private void OnBgAlphaTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                if (string.IsNullOrEmpty(BgColorAlphaText.Text))
                {
                    BgColorAlphaText.Value = 0;
                    BgColorAlpha.Value = 0;
                }
                else
                {
                    BgColorAlpha.Value = (double)BgColorAlphaText.Value;
                }
            }
        }
        private void OnBgImageAlphaTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (string.IsNullOrEmpty(BgAlphaText.Text))
            {
                BgAlphaText.Value = 0;
                BgAlpha.Value = 0;
            }
            else
            {
                BgAlpha.Value = (double)BgAlphaText.Value;

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectBgButton.IsEnabled = false;
            var res = ResourceWindow.SelectImage(window);
            SelectBgButton.IsEnabled = true;
            if (res is not null)
            {
                window.MessageWindowBgImage.Source = Utils.CreateBmp(res);
                window.Bg = Path.GetFileName(res);
                BgAlpha.IsEnabled = true;
                BgAlphaText.IsEnabled = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            window.MessageWindowBgImage.Source = null;
            window.Bg = "";
            BgAlpha.IsEnabled = false;
            BgAlphaText.IsEnabled = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            window.WindowContents.ClearValue(Canvas.RightProperty);
            Canvas.SetLeft(window.WindowContents, 0);
            MessageWindowWidth.Text = MainWindow.pmanager.Project.Size[0].ToString();
        }

        private void OnWidthTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                window.WindowContents.Width = (double)MessageWindowWidth.Value;
            }
        }

        private void OnHeightTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                window.WindowContents.Height = (double)MessageWindowHeight.Value;
            }
        }
    }
}
