using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// CharaSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class CharaSettingPage : Page
    {
        public CharaSettingPage()
        {
            InitializeComponent();
            Triming.IsChecked = MainWindow.pmanager.Project.CharacterSettings.Triming;
        }

        private void Triming_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.CharacterSettings.Triming = Triming.IsChecked ?? false;
        }
    }
}
