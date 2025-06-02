using EmeralEngine.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmeralEngine.Project
{
    /// <summary>
    /// SelectProjectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectProjectWindow : Window
    {
        private MainWindow parent;
        private string SelectedProject;
        public SelectProjectWindow(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            Owner = parent;
            Load();
        }
        private void Load()
        {
            var row = 0;
            var row_h = new GridLength(85);
            foreach (var p in MainWindow.pmanager.GetProjectNames())
            {
                var panel = new DockPanel()
                {
                    Background = CustomColors.CharacterBackground,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 400,
                    Height = 80,
                    LastChildFill = false
                };
                panel.MouseEnter += (sender, e) =>
                {
                    panel.Opacity = 0.7;
                };
                panel.MouseLeave += (sender, e) =>
                {
                    panel.Opacity = 1;
                };
                panel.MouseLeftButtonDown += (sender, e) => {
                    SelectedProject = p;
                    LoadProject();
                };
                var grid = new Grid();
                var img = new Image()
                {
                    Width = 50,
                    Height = 50,
                };
                var name = new Label()
                {
                    Content = p,
                    FontSize = 20,
                    Foreground = Brushes.Black
                };
                Grid.SetColumn(img, 0);
                Grid.SetColumn(name, 1);
                grid.Children.Add(img);
                grid.Children.Add(name);
                panel.Children.Add(grid);
                Projects.RowDefinitions.Add(new RowDefinition()
                {
                    Height = row_h
                });
                Grid.SetRow(panel, row);
                Projects.Children.Add(panel);
                row++;
            }
        }
        private void LoadProject()
        {
            Close();
            parent.LoadProject(SelectedProject);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new NewProjectWindow(parent).ShowDialog();
        }
    }
}
