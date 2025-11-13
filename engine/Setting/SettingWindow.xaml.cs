using EmeralEngine.Core;
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
using System.Windows.Shapes;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        private DockPanel now_setting;
        private Page projectPage, startupPage, editorPage, scenePage, charaPage, exportPage;
        public SettingWindow(Window parent)
        {
            InitializeComponent();
            Owner = parent;
            projectPage = new ProjectPage(this);
            startupPage = new StartupPage();
            editorPage = new EditorPage();
            scenePage = new SceneSettingPage();
            charaPage = new CharaSettingPage();
            exportPage = new ExportPage();
            now_setting = ProjectPanel;
            Frame.Navigate(projectPage);
        }
        private void ChangePage(DockPanel panel, Page page)
        {
            now_setting.Background = null;
            now_setting = panel;
            now_setting.Background = CustomColors.FocusingSetting;
            Frame.Navigate(page);
        }

        private void OnStartupPanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(StartupPanel, startupPage);
        }

        private void OnCharaPanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(CharaPanel, charaPage);
        }

        private void OnEditorPanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(EditorPanel, editorPage);
        }

        private void OnProjectPanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(ProjectPanel, projectPage);
        }

        private void OnScenePanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(ScenePanel, scenePage);
        }

        private void OnExportPanelMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            ChangePage(ExportPanel, exportPage);
        }
    }
}
