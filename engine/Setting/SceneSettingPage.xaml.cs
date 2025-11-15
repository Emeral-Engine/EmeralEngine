using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// SceneSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SceneSettingPage : Page
    {
        public SceneSettingPage()
        {
            InitializeComponent();
            ChangeLatterBg.IsChecked = MainWindow.pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged;
        }

        private void OnChangeLatterBgClicked(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged = ChangeLatterBg.IsChecked ?? false;
        }
    }
}
