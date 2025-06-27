using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine
{
    /// <summary>
    /// MessageWindowSelectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectMessageWindow : Window
    {
        public int Result;
        public SelectMessageWindow()
        {
            InitializeComponent();
            var row = 0;
            var manager = new MessageWindowManager();
            Result = -1;
            foreach (var p in manager.windows)
            {
                var i = row;
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
                panel.MouseDown += (sender, e) =>
                {
                    Result = i;
                    Close();
                };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                var builder = new MessageWindowBuilder(manager[p]);
                var preview = builder.Build();
                Grid.SetColumn(preview, 0);
                grid.Children.Add(preview);
                var name = new Label()
                {
                    Content = Path.GetFileNameWithoutExtension(p),
                    FontSize = 20,
                };
                Grid.SetColumn(name, 1);
                grid.Children.Add(name);
                panel.Children.Add(grid);
                WindowGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(100)
                });
                Grid.SetRow(panel, row);
                WindowGrid.Children.Add(panel);
                row++;
            }
        }

        public static int Select(Window w)
        {
            var window = new SelectMessageWindow();
            window.Owner = w;
            window.ShowDialog();
            return window.Result;
        }
    }
}
