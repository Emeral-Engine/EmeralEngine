using EmeralEngine.Core;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace EmeralEngine.Story
{
    /// <summary>
    /// EpisodeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class EpisodeWindow : Window
    {
        private EpisodeManager emanager;
        public EpisodeInfo Selection;
        private string pre_text;
        private bool IsPressed, IsSelectMode;
        public EpisodeWindow(Window w)
        {
            InitializeComponent();
            Owner = w;
            emanager = new();
            Load();
        }

        private void Load()
        {
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            var row = 0;
            foreach (var ep in emanager.GetEpisodes())
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
                panel.MouseLeftButtonDown += (sender, e) =>
                {
                    IsPressed = true;
                };
                panel.MouseLeftButtonUp += (sender, e) =>
                {
                    if (IsSelectMode && IsPressed)
                    {
                        Selection = ep;
                        Close();
                    }
                };
                var grid = new UniformGrid()
                {
                    Rows = 1,
                    Columns = 2,
                };
                var img = new Image()
                {
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                var label = new TextBox()
                {
                    Text = ep.Name,
                    FontSize = 20,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Width = 150,
                    IsEnabled = false,
                    BorderBrush = CustomColors.Transparent,
                    IsReadOnly = true
                };
                label.LostFocus += (sender, e) =>
                {
                    label.Text = pre_text;
                    label.IsEnabled = false;
                    label.IsReadOnly = true;
                    label.BorderBrush = CustomColors.Transparent;
                };
                label.GotFocus += (sender, e) =>
                {
                    label.IsReadOnly = false;
                    var length = label.Text.Count();
                    label.Select(0, length);
                    pre_text = label.Text;
                };
                label.TextChanged += (sender, e) =>
                {
                    label.BorderBrush = emanager.GetEpisode(label.Text) is null ? CustomColors.Transparent : CustomColors.WarnBorder;
                };
                label.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter && !string.IsNullOrEmpty(label.Text) && emanager.GetEpisode(label.Text) is null
                       && MessageBox.Show("名前を変更する場合、参照は手動で再設定し直してください", "EmeralEngine", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        emanager.Rename(ep.Name, label.Text);
                        label.ReleaseAllTouchCaptures();
                        Load();
                    }
                };
                grid.Children.Add(label);
                grid.Children.Add(img);
                panel.Children.Add(grid);
                MainGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(100)
                });
                Grid.SetRow(panel, row);
                MainGrid.Children.Add(panel);
                var menu = new ContextMenu();
                var item = new MenuItem()
                {
                    Header = "名前の変更"
                };
                item.Click += (sender, e) =>
                {
                    label.IsEnabled = true;
                    label.Focus();
                };
                var item2 = new MenuItem()
                {
                    Header = "削除"
                };
                item2.Click += (sender, e) =>
                {
                    Directory.Delete(ep.Path, true);
                    Load();
                };
                menu.Items.Add(item);
                menu.Items.Add(item2);
                panel.ContextMenu = menu;
                row++;
                Task.Run(() =>
                {
                    var bmp = ep.GetThumbnail();
                    Dispatcher.Invoke(() =>
                    {
                        img.Source = bmp;
                    });
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var w = new NewEpisodeWindow(this);
            w.ShowDialog();
            if (w.New is not null)
            {
                Load();
            }
        }

        public void Select()
        {
            IsSelectMode = true;
            ShowDialog();
        }
    }
}
