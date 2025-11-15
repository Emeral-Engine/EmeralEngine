using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// StartupPage.xaml の相互作用ロジック
    /// </summary>
    public partial class StartupPage : Page
    {
        private bool _selected
        {
            get => NavigationService is not null;
        }
        public StartupPage()
        {
            InitializeComponent();
            EpisodeWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Story;
            SceneWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Scene;
            ScriptWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Script;
            ResourceWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Resource;
            CharaWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Chara;
            MessageWindowCheck.IsChecked = MainWindow.pmanager.Project.Startup.Msw;
        }

        private void EpisodeWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Story = (bool)EpisodeWindowCheck.IsChecked;
        }

        private void SceneWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Scene = (bool)SceneWindowCheck.IsChecked;
        }

        private void ScriptWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Script = (bool)ScriptWindowCheck.IsChecked;
        }

        private void ResourceWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Resource = (bool)ResourceWindowCheck.IsChecked;
        }

        private void CharaWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Chara = (bool)CharaWindowCheck.IsChecked;
        }

        private void MessageWindowCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_selected) return;
            MainWindow.pmanager.Project.Startup.Msw = (bool)MessageWindowCheck.IsChecked;
        }
    }
}
