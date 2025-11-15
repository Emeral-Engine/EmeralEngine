using EmeralEngine.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Resource
{
    /// <summary>
    /// ResourceWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ResourceWindow : Window
    {
        private int row;
        private int types;
        private bool select_mode;
        public string? SelectResult;
        public ResourceWindow(Window w)
        {
            InitializeComponent();
            Owner = w;
            types = FileTypes.ALL;
        }
        public void Load()
        {
            ResourceGrid.Children.Clear();
            ResourceGrid.RowDefinitions.Clear();
            row = 0;
            foreach (var f in MainWindow.pmanager.GetResources())
            {
                DrawPanel(f);
            }
        }

        public static string? SelectImage(Window w)
        {
            return (new ResourceWindow(w)).SelectImage();
        }
        public string? SelectImage()
        {
            ResourceGrid.Children.Clear();
            ResourceGrid.RowDefinitions.Clear();
            SelectResult = null;
            row = 0;
            types = FileTypes.IMAGE;
            select_mode = true;
            foreach (var f in MainWindow.pmanager.GetResources())
            {
                if (Utils.IsImage(f)) DrawPanel(f);
            }
            ShowDialog();
            return SelectResult;
        }
        public static string? SelectMovie(Window w)
        {
            return (new ResourceWindow(w)).SelectMovie();
        }
        public string? SelectMovie()
        {
            ResourceGrid.Children.Clear();
            ResourceGrid.RowDefinitions.Clear();
            SelectResult = null;
            row = 0;
            types = FileTypes.MOVIE;
            select_mode = true;
            foreach (var f in MainWindow.pmanager.GetResources())
            {
                if (Utils.IsMovie(f))
                {
                    DrawPanel(f);
                }
            }
            ShowDialog();
            return SelectResult;
        }
        public static string? SelectAudio(Window w)
        {
            return (new ResourceWindow(w)).SelectAudio();
        }
        public string? SelectAudio()
        {
            ResourceGrid.Children.Clear();
            ResourceGrid.RowDefinitions.Clear();
            SelectResult = null;
            row = 0;
            types = FileTypes.AUDIO;
            select_mode = true;
            foreach (var f in MainWindow.pmanager.GetResources())
            {
                if (Utils.IsAudio(f)) DrawPanel(f);
            }
            ShowDialog();
            return SelectResult;
        }
        private void DrawPanel(string f)
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
            if (select_mode)
            {
                panel.MouseDown += (sender, e) =>
                {
                    SelectResult = f;
                    Close();
                };
            }
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var name = new Label()
            {
                Content = Utils.CutString(Path.GetFileName(f), 18),
                FontSize = 20,
            };
            if (Utils.IsImage(f))
            {
                var img = new Image()
                {
                    Source = Utils.CreateBmp(f),
                    Width = 100,
                    Height = 100,
                };
                Grid.SetColumn(img, 0);
                grid.Children.Add(img);
            }
            Grid.SetColumn(name, 1);
            grid.Children.Add(name);
            panel.Children.Add(grid);
            ResourceGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(100)
            });
            Grid.SetRow(panel, row);
            ResourceGrid.Children.Add(panel);
            row++;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var pathes = e.Data.GetData(DataFormats.FileDrop) as string[];
            foreach (var p in pathes)
            {
                var res = Utils.GetUnusedFileName(MainWindow.pmanager.GetResource(Path.GetFileName(p)));
                File.Copy(p, res);
                DrawPanel(res);
            }
            try
            {
            }
            catch { }
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
                switch (types)
                {
                    case FileTypes.AUDIO:
                        e.Effects = ((string[])e.Data.GetData(DataFormats.FileDrop))
                            .All(Utils.IsAudio) ? DragDropEffects.Copy : DragDropEffects.None;
                        break;
                    case FileTypes.IMAGE:
                        e.Effects = ((string[])e.Data.GetData(DataFormats.FileDrop))
                            .All(Utils.IsImage) ? DragDropEffects.Copy : DragDropEffects.None;
                        break;
                    case FileTypes.MOVIE:
                        e.Effects = ((string[])e.Data.GetData(DataFormats.FileDrop))
                            .All(Utils.IsMovie) ? DragDropEffects.Copy : DragDropEffects.None;
                        break;
                    default:
                        e.Effects = DragDropEffects.Copy;
                        break;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }

    struct FileTypes
    {
        public const int ALL = 0;
        public const int AUDIO = 1;
        public const int IMAGE = 2;
        public const int MOVIE = 3;
    }
}
