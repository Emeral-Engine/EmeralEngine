using EmeralEngine.Scene;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmeralEngine
{
    /// <summary>
    /// SceneWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SceneWindow : Window
    {
        private const int LINE_WIDTH = 100;
        private const int LINE_HEIGHT = 30;
        const int PANEL_WIDTH = 300;
        const int PANEL_HEIGHT = 200;
        private Thickness border_size = new Thickness(5);
        private double panel_x, panel_y, line_y;
        private double defaultWindowWidth, defaultWindowHeight;
        private int edgeCount;
        private double heightRate;
        private MainWindow parent;
        private Border focusing_border;
        public SceneInfo now_scene;
        private SceneInfo pre_info;
        private RenderTargetBitmap black_image
        {
            get
            {
                var rect = new Rectangle()
                {
                    Width = PANEL_WIDTH,
                    Height = PANEL_HEIGHT,
                    Fill = Brushes.Black
                };
                var size = new Size(PANEL_WIDTH, PANEL_HEIGHT);
                rect.Measure(size);
                rect.Arrange(new Rect(size));
                var b = new RenderTargetBitmap(100, 200, 96, 96, PixelFormats.Pbgra32);
                b.Render(rect);
                return b;
            }
        }
        public SceneWindow(MainWindow p)
        {
            InitializeComponent();
            parent = p;
            Owner = parent;
            defaultWindowWidth = Width;
            defaultWindowHeight = Height;
            ContentRendered += (sender, e) =>
            {
                Load();
            };
        }
        public void Load()
        {
            Screens.Children.Clear();
            pre_info = null;
            Screens.Width = 0;
            edgeCount = 0;
            panel_x = 0;
            panel_y = ActualHeight / 4;
            line_y = ActualHeight / 2.5;
            var n = 1;
            foreach (var i in parent.now_episode.smanager.scenes)
            {
                DrawPanel($"シーン{n}", i.Value);
                n++;
            }
        }
        private (Border, Image, SceneInfo) DrawPanel(string name, SceneInfo info, bool begin=false)
        {
            var isFirst = pre_info is null;
            var i = pre_info;
            if (!isFirst)
            {
                var line = EdgeLine;
                var l_ctx = new ContextMenu();
                var l_item1 = new MenuItem()
                {
                    Header = "トランジション"
                };
                l_item1.Click += (sender, e) =>
                {
                    (new TransitionWindow(this, i)).Show();
                };
                l_ctx.Items.Add(l_item1);
                line.ContextMenu = l_ctx;
                Canvas.SetLeft(line, panel_x);
                Canvas.SetTop(line, line_y);
                Screens.Children.Add(line);
                Screens.Width += line.Width;
                panel_x += line.Width;
            }
            var grid = new Grid();
            var border = new Border()
            {
                BorderBrush = CustomColors.FocusingScene,
            };
            var panel = new DockPanel()
            {
                Height = PANEL_HEIGHT,
                Width = PANEL_WIDTH,
            };
            panel.MouseEnter += (sender, e) =>
            {
                panel.Opacity = 0.7;
            };
            panel.MouseLeave += (sender, e) =>
            {
                panel.Opacity = 1;
            };
            var img = new Image()
            {
                Source = 0 < info.bg.Length ? info.thumbnail : black_image,
                Stretch = Stretch.Fill
            };
            if (parent.now_scene == info)
            {
                img.Loaded += (sender, e) =>
                {
                    border.Width = img.ActualWidth;
                    border.Height = img.ActualHeight;
                    FocusScene(border, img, info);
                };

            }
            else
            {
                img.Loaded += (sender, e) => {
                    border.Width = img.ActualWidth;
                    border.Height = img.ActualHeight;
                };
            }
            panel.MouseLeftButtonDown += (sender, e) =>
            {
                if (now_scene != info) FocusScene(border, img, info);
            };
            grid.Children.Add(img);
            var text = new Label()
            {
                Content = name,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = Brushes.Black,
                Background = Brushes.White,
                FontSize = 20,
                Opacity = 0.7
            };
            grid.Children.Add(text);
            border.Child = grid;
            panel.Children.Add(border);
            Canvas.SetLeft(panel, panel_x);
            Canvas.SetTop(panel, panel_y);
            var ctx = new ContextMenu();
            var item1 = new MenuItem()
            {
                Header = "背景の変更"
            };
            item1.Click += (sender, e) =>
            {
                parent.SelectBg();
            };
            ctx.Items.Add(item1);
            var item2 = new MenuItem()
            {
                Header = "BGMの変更"
            };
            item2.Click += (sender, e) =>
            {
                parent.SelectBgm();
            };
            ctx.Items.Add(item2);
            ctx.Items.Add(new Separator());
            var item3 = new MenuItem()
            {
                Header = "削除"
            };
            item3.Click += (sender, e) =>
            {
                File.Delete(info.path);
                parent.now_episode.smanager.scenes.Remove(info.order);
                Load();
            };
            ctx.Items.Add(item3);
            panel.ContextMenu = ctx;
            Screens.Children.Add(panel);
            Screens.Width += panel.Width;
            panel_x += panel.Width;
            pre_info = info;
            return (border, img, info);
        }
        private Rectangle EdgeLine
        {
            get
            {
                edgeCount += 1;
                var line = new Rectangle()
                {
                    Height = LINE_HEIGHT,
                    Width = LINE_WIDTH,
                    Fill = CustomColors.Edge
                };
                line.MouseEnter += (sender, e) => {
                    line.Opacity = 0.7;
                };
                line.MouseLeave += (sender, e) =>
                {
                    line.Opacity = 1;
                };
                var ctx = new ContextMenu();
                var item1 = new MenuItem()
                {
                    Header = "分岐"
                };
                ctx.Items.Add(item1);
                line.ContextMenu = ctx;
                return line;
            }
        }
        private void _NewScene(object sender, RoutedEventArgs e)
        {
            NewScene();
        }
        private void NewScene()
        {
            var info = parent.now_episode.smanager.New();
            var res = DrawPanel($"シーン{info.order}", info);
            res.Item2.Loaded += (sender, e) =>
            {
                FocusScene(res.Item1, res.Item2, res.Item3);
            };
        }
        private void FocusScene(Border border, Image img, SceneInfo info)
        {
            if (focusing_border is not null)
            {
                focusing_border.BorderThickness = new Thickness(0);
                border.Height = img.ActualHeight;
            }
            focusing_border = border;
            focusing_border.BorderThickness = border_size;
            border.Height = img.ActualHeight + 5;
            now_scene = info;
            parent.ChangeScene(now_scene);
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scale.ScaleX = ActualWidth / defaultWindowWidth;
            Scale.ScaleY = ActualHeight / defaultWindowHeight;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer1.ScrollToHorizontalOffset(ScrollViewer1.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    break;
            }
        }
    }
}
