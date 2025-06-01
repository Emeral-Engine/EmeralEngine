using EmeralEngine.Resource;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// ProjectPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ProjectPage : Page
    {
        private bool _selected
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
        }

        private void OnGameWidthValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Size[0] = GameWidth.Value ?? 0;
        }

        private void OnGameHeightValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!_selected) return;
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
    }
}
