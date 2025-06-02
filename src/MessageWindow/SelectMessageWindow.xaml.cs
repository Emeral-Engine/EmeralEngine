using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

namespace EmeralEngine
{
    /// <summary>
    /// MessageWindowSelectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectMessageWindow : Window
    {
        public MessageWindowConfig Result;
        public int SelectedNum;
        public SelectMessageWindow(MainWindow parent, Dictionary<int, MessageWindowConfig> windows)
        {
            InitializeComponent();
            Owner = parent;
            var row = 0;
            foreach (var k in windows)
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
                panel.MouseDown += (sender, e) =>
                {
                    Result = k.Value;
                    SelectedNum = k.Key;
                    Close();
                };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                var builder = new MessageWindowBuilder(k.Value);
                var preview = builder.Build();
                Grid.SetColumn(preview, 0);
                grid.Children.Add(preview);
                var name = new Label()
                {
                    Content = k.Key,
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
    }
}
