using EmeralEngine.Resource;
using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// ProjectPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ProjectPage : Page
    {
        private bool IsSelected
        {
            get => NavigationService is not null;
        }

        private Window window;
        public ProjectPage(Window w)
        {
            InitializeComponent();
            window = w;
            GameWidth.Value = MainWindow.pmanager.Project.Size[0];
            GameHeight.Value  = MainWindow.pmanager.Project.Size[1];
            MouseOverSE.Text = MainWindow.pmanager.Project.MouseOverSE;
            MouseDownSE.Text = MainWindow.pmanager.Project.MouseDownSE;
            TextInterval.Value = MainWindow.pmanager.Project.TextInterval;
            ScalingMode.SelectedIndex = MainWindow.pmanager.Project.ScalingMode;
        }

        private void OnGameWidthValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsSelected) return;
            MainWindow.pmanager.Project.Size[0] = GameWidth.Value ?? 0;
        }

        private void OnGameHeightValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsSelected) return;
            MainWindow.pmanager.Project.Size[1] = GameHeight.Value ?? 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var res = ResourceWindow.SelectAudio(window);
            if (!string.IsNullOrEmpty(res))
            {
                var p = MainWindow.pmanager.RelToResourcePath(res);
                MouseOverSE.Text = p;
                MainWindow.pmanager.Project.MouseOverSE = p;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var res = ResourceWindow.SelectAudio(window);
            if (!string.IsNullOrEmpty(res))
            {
                var p = MainWindow.pmanager.RelToResourcePath(res);
                MouseDownSE.Text = p;
                MainWindow.pmanager.Project.MouseDownSE = p;
            }
        }

        private void OnIntervalValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsSelected)
            {
                MainWindow.pmanager.Project.TextInterval = (int)TextInterval.Value;
            }
        }

        private void ScalingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSelected)
            {
                MainWindow.pmanager.Project.ScalingMode = ScalingMode.SelectedIndex;
            }
        }
    }
}
