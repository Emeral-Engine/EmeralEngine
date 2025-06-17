using EmeralEngine.Core;
using EmeralEngine.Resource;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmeralEngine.MessageWindow
{
    /// <summary>
    /// NamePlateSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class NamePlateSettingPage : Page
    {
        public string BgImage;
        private MessageWindowDesigner window;
        private bool IsLoaded;
        public NamePlateSettingPage(MessageWindowDesigner w)
        {
            InitializeComponent();
            window = w;
            BgImage = "";
            Loaded += (sender, e) =>
            {
                IsLoaded = true;
            };
        }

        private void OnSelectColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            window.NamePlate.Background = Utils.GetBrush(BgColor.SelectedColorText);
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                window.NamePlate.Background.Opacity = e.NewValue;
                BgColorAlphaText.Value = e.NewValue;
            }
        }

        private void OnBgAlphaTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                var v = (double)e.NewValue;
                window.NamePlate.Background.Opacity = v;
                BgColorAlpha.Value = v;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var img = ResourceWindow.SelectImage(window);
            if (!string.IsNullOrEmpty(img))
            {
                window.NamePlateBgImage.Source = Utils.CreateBmp(img);
                BgImage = Path.GetFileName(img);
                DeleteBgButton.IsEnabled = true;
                BgImageAlpha.IsEnabled = true;
                BgImageAlphaText.IsEnabled = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            window.NamePlateBgImage.Source = null;
            BgImage = "";
            DeleteBgButton.IsEnabled = false;
            BgImageAlpha.IsEnabled = false;
            BgImageAlphaText.IsEnabled = false;
        }

        private void OnBgSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                window.NamePlateBgImage.Opacity = e.NewValue;
                BgImageAlphaText.Value = e.NewValue;
            }
        }

        private void OnBgImageAlphaTextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                var v = (double)e.NewValue;
                window.NamePlateBgImage.Opacity = v;
                BgImageAlpha.Value = v;
            }
        }

        private void FontList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                window.CharaName.FontFamily = new FontFamily(FontList.Text);
            }
        }

        private void OnNameColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (IsLoaded)
            {
                window.CharaName.Foreground = Utils.GetBrush(NameColor.SelectedColorText);
            }
        }

        private void OnFontSizeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                window.CharaName.FontSize = (int)e.NewValue;
            }
        }
    }
}
