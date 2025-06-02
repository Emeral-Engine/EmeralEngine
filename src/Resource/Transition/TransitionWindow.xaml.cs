using EmeralEngine.Core;
using EmeralEngine.Resource;
using EmeralEngine.Resource.CustomTransition;
using EmeralEngine.Resource.Transition;
using EmeralEngine.Scene;
using System.Windows;

namespace EmeralEngine
{
    /// <summary>
    /// TransitionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TransitionWindow : Window
    {
        public const double DEFAULT_FADE_TIME = 5.0;
        public bool IsRendered;
        public BaseInfo info;
        private MonoTransitionPage monoPage;
        public TransitionWindow(Window w, BaseInfo i)
        {
            InitializeComponent();
            Owner = w;
            info = i;
            monoPage = new(this);
            if (info.trans == TransitionTypes.SIMPLE)
            {
                TransitionType.SelectedItem = TypeMono;
            }
            ContentRendered += (sender, e) => {
                IsRendered = true;
                Interval.Value = info.interval;
            };
        }

        private void OnNoneSelected(object sender, RoutedEventArgs e)
        {
            if (IsRendered)
            {
                TransitionSetting.Content = null;
                info.trans = TransitionTypes.NONE;
                info.fadein = 0;
                info.fadeout = 0;
            }
        }

        private void OnMonoSelected(object sender, RoutedEventArgs e)
        {
            TransitionSetting.Navigate(monoPage);
        }

        private void OnIntervalChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsRendered)
            {
                info.interval = Interval.Value ?? 0;
            }
        }
    }
}
