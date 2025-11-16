using EmeralEngine.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        private const int DEFAULT_WIDTH = 800;
        private const int DEFAULT_HEIGHT = 450;
        private DockPanel now_setting;
        private Page projectPage, startupPage, editorPage, scenePage, charaPage, exportPage;
        public SettingWindow(Window parent)
        {
            InitializeComponent();
            Owner = parent;
            Width *= 1.5;
            Height *= 1.5;
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

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var r = Math.Min(ActualWidth / DEFAULT_WIDTH, ActualHeight / DEFAULT_HEIGHT);
            Scale.ScaleX = r;
            Scale.ScaleY = r;
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
