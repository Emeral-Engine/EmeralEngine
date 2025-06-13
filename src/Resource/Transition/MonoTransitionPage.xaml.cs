using EmeralEngine.Resource.CustomTransition;
using EmeralEngine.Scene;
using System;
using System.Collections.Generic;
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
using Xceed.Wpf.Toolkit.Core.Media;

namespace EmeralEngine.Resource.Transition
{
    /// <summary>
    /// MonoTransitionPage.xaml の相互作用ロジック
    /// </summary>
    public partial class MonoTransitionPage : Page
    {
        private TransitionWindow parent;
        public MonoTransitionPage(TransitionWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            Loaded += (sender, e) =>
            {
                MonochroColor.SelectedColor = string.IsNullOrEmpty(parent.info.trans_color) ? System.Windows.Media.Colors.Black : (Color)ColorConverter.ConvertFromString(parent.info.trans_color);
                Fadein.Value = 0 < parent.info.fadein ? parent.info.fadein : TransitionWindow.DEFAULT_FADE_TIME;
                Fadeout.Value = 0 < parent.info.fadeout ? parent.info.fadeout : TransitionWindow.DEFAULT_FADE_TIME;
                parent.info.trans = TransitionTypes.SIMPLE;
            };
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (parent is not null && parent.IsRendered)
            {
                parent.info.fadein = Fadein.Value ?? TransitionWindow.DEFAULT_FADE_TIME;
                parent.info.fadeout = Fadeout.Value ?? TransitionWindow.DEFAULT_FADE_TIME;
            }
        }

        private void OnColorChabged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (parent is not null && parent.IsRendered)
            {
                parent.info.trans_color = MonochroColor.SelectedColor.ToString();
            }
        }
    }
}
