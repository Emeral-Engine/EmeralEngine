using EmeralEngine.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmeralEngine.MessageWindow
{
    /// <summary>
    /// ScriptSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ScriptSettingPage : Page
    {
        private MessageWindowDesigner window;
        public ScriptSettingPage(MessageWindowDesigner w)
        {
            InitializeComponent();
            window = w;
        }

        private void OnTextColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            window.Script.Foreground = Utils.GetBrush(TextColorPicker.SelectedColorText);
        }

        private void OnFontSizeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                window.Script.FontSize = (int)FontSize.Value;
                var size = TextHelper.GetTextHeight(MessageWindowDesigner.SAMPLE_SCRIPT, window.Script);
            }
        }
    }
}
