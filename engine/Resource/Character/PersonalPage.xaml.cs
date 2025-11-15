using EmeralEngine.Core;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace EmeralEngine.Resource.Character
{
    /// <summary>
    /// PersonalPage.xaml の相互作用ロジック
    /// </summary>
    public partial class PersonalPage : Page
    {
        private CharacterWindow cwindow;
        private bool IsSelectMode, IsPressed;
        private string name;
        private string pre_text;
        public PersonalPage(CharacterWindow w)
        {
            InitializeComponent();
            cwindow = w;
        }
        public void SetName(string n)
        {
            name = n;
            CharacterName.Content = name;
            Title = name;
            Load();
        }
        private void Load()
        {
            CharacterImagesGrid.Children.Clear();
            CharacterImagesGrid.RowDefinitions.Clear();
            var row = 0;
            foreach (var s in cwindow.cmanager.GetCharacterPictureFiles(name))
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
                        cwindow.SelectedPicture = s;
                        cwindow.Hide();
                        cwindow = null;
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
                    Text = Path.GetFileName(s),
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
                    var length = Path.GetFileNameWithoutExtension(label.Text).Count();
                    label.Select(0, length);
                    pre_text = label.Text;
                };
                label.TextChanged += (sender, e) =>
                {
                    label.BorderBrush = cwindow.cmanager.ExistsPictureName(name, label.Text) ? CustomColors.WarnBorder : CustomColors.Transparent;
                };
                label.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter && !cwindow.cmanager.ExistsPictureName(name, label.Text))
                    {
                        cwindow.cmanager.MovePicture(name, Path.GetFileName(s), label.Text);
                        label.ReleaseAllTouchCaptures();
                        Load();
                    }
                };
                grid.Children.Add(label);
                grid.Children.Add(img);
                panel.Children.Add(grid);
                CharacterImagesGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(100)
                });
                Grid.SetRow(panel, row);
                CharacterImagesGrid.Children.Add(panel);
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
                    File.Delete(s);
                    Load();
                };
                menu.Items.Add(item);
                menu.Items.Add(item2);
                panel.ContextMenu = menu;
                row++;
                Task.Run(() =>
                {
                    var bmp = Utils.CreateBmp(s);
                    Dispatcher.Invoke(() =>
                    {
                        img.Source = bmp;
                    });
                });
            }
        }
        public void Select(CharacterWindow w)
        {
            IsSelectMode = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files is not null)
            {
                foreach (var f in files)
                {
                    cwindow.cmanager.AddPicture(name, f);
                }
                Load();
            }
            e.Handled = true;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            JudgeDrop(e);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            JudgeDrop(e);
        }

        private void JudgeDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = (e.Data.GetData(DataFormats.FileDrop) as string[])
                    .All(Utils.IsImage) ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Multiselect = true
            };
            dlg.Filter = "画像ファイル|*.png;*.ping;*.jpg;*.jpeg;*.bmp;*.tiff";
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    cwindow.cmanager.AddPicture(name, f);
                }
                Load();
            }
        }
    }
}
