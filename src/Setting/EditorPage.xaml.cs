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
