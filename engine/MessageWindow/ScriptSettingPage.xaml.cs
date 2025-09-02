using EmeralEngine.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
