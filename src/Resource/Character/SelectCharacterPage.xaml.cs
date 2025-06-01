using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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

namespace EmeralEngine.Resource.Character
{
    /// <summary>
    /// SelectCharacterPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectCharacterPage : Page
    {
        private PersonalPage pPage;
        private CharacterWindow parent;
        private Frame frame;
        public SelectCharacterPage(CharacterWindow p, Frame f)
        {
            InitializeComponent();
            parent = p;
            frame = f;
            pPage = new(parent);
            ListUp();
        }
        private void ListUp()
        {
            CharacterGrid.Children.Clear();
            CharacterGrid.RowDefinitions.Clear();
            var row = 0;
            foreach (var c in parent.cmanager.GetCharacterNames())
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
                panel.MouseDown += (sender, e) => {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        pPage.SetName(c);
                        frame.Navigate(pPage);
                    }
                };
                var grid = new Grid();
                var img = new Image()
                {
                    Width = 50,
                    Height = 50,
                };
                var name = new Label()
                {
                    Content = c,
                    FontSize = 20,
                };
                Grid.SetColumn(img, 0);
                Grid.SetColumn(name, 1);
                grid.Children.Add(img);
                grid.Children.Add(name);
                panel.Children.Add(grid);
                CharacterGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(100)
                });
                Grid.SetRow(panel, row);
                CharacterGrid.Children.Add(panel);
                row++;
            }
        }
        public void SelectMode()
        {
            pPage.Select(parent);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (new NewCharacter(parent)).ShowDialog();
            ListUp();
        }
    }
}
