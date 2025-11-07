using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace EmeralEngine.Project
{
    /// <summary>
    /// NewProjectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewProjectWindow : Window
    {
        private MainWindow parent;
        public NewProjectWindow(MainWindow w)
        {
            InitializeComponent();
            parent = w;
            ProjectTitle.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.pmanager.GetProjectNames().Contains(ProjectTitle.Text))
            {
                MessageBox.Show("この名前のプロジェクトは既に存在しています", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int[] size;
            if ((bool)WindowSize1.IsChecked)
            {
                size = new int[] {960, 640};
            }else if ((bool)WindowSize2.IsChecked)
            {
                size = new int[] {1280, 720};
            }
            else
            {
                size = new int[] { WindowWidth.Value ?? 0, WindowHeight.Value ?? 0 };
            }
            var p = new CreateProjectProgress();
            var title = ProjectTitle.Text;
            p.Start(new Task(() =>
            {
                parent.NewProject(title, size);
                Dispatcher.Invoke(Close);
            }), () =>
            {
                parent.LoadProject(Path.Combine(ProjectManager.ProjectsDir, title, "project.emeral"));
            });
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CreateButton.IsEnabled = !string.IsNullOrEmpty(ProjectTitle.Text) && (!(bool)CustomSize.IsChecked || (!string.IsNullOrEmpty(WindowWidth.Text) && !string.IsNullOrEmpty(WindowHeight.Text)));
        }

        private void OnCustomChecked(object sender, RoutedEventArgs e)
        {
            WindowHeight.IsEnabled = true;
            WindowWidth.IsEnabled = true;
            CreateButton.IsEnabled = !string.IsNullOrEmpty(ProjectTitle.Text) && !string.IsNullOrEmpty(WindowWidth.Text) && !string.IsNullOrEmpty(WindowHeight.Text);
        }

        private void OnCustomUnchecked(object sender, RoutedEventArgs e)
        {
            WindowHeight.IsEnabled = false;
            WindowWidth.IsEnabled =false;
            CreateButton.IsEnabled = !string.IsNullOrEmpty(ProjectTitle.Text) && (!(bool)CustomSize.IsChecked || (!string.IsNullOrEmpty(WindowWidth.Text) && !string.IsNullOrEmpty(WindowHeight.Text))); ;
        }
    }
}
