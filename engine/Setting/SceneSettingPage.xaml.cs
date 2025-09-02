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
