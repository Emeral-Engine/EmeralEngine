using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// EditorSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class EditorPage : Page
    {
        public EditorPage()
        {
            InitializeComponent();
            NewScriptCheck.IsChecked = MainWindow.pmanager.Project.EditorSettings.AddScriptWhenEmpty;
        }

        private void NewScriptCheck_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.EditorSettings.AddScriptWhenEmpty = NewScriptCheck.IsChecked ?? false;
        }
    }
}
