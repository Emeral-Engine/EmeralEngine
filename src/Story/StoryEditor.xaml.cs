using EmeralEngine.Core;
using EmeralEngine.Resource;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmeralEngine.Story
{
    /// <summary>
    /// ContextEditor.xaml の相互作用ロジック
    /// </summary>
    public partial class StoryEditor : Window
    {
        const int CONTENT_X_SPACE = 50;
        const int CONTENT_Y_SPACE = 80;
        const int PANEL_WIDTH = 300;
        const int PANEL_HEIGHT = 200;
        private const int LINE_WIDTH = 100;
        private const int LINE_HEIGHT = 30;
        private bool _IsBranchRender;
        private StoryManager story;
        private List<ContentInfo> contents;
        private Thickness border_size = new Thickness(5);
        private double panelX, defaultPanelY, panelY, lineY;
        private double defaultWindowWidth, defaultWindowHeight;
        private int edgeCount;
        private double heightRate;
        private MainWindow parent;
        private Border focusing_border;
        public ContentInfo now_content;
        private ContentInfo pre_info;
        private EpisodeManager emanager;
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
        public StoryEditor(MainWindow parent)
        {
            InitializeComponent();
            Owner = parent;
            this.parent = parent;
            story = parent.story;
            emanager = parent.emanager;
            contents = new();
            Load();
        }
        public void Load()
        {
            pre_info = null;
            Screens.Children.Clear();
            Screens.Width = 0;
            edgeCount = 0;
            panelX = 0;
            defaultPanelY = Height / 4;
            panelY = defaultPanelY;
            lineY = Height / 2.5;
            emanager.Dump();
            foreach (var s in story.StoryInfos)
            {
                if (s.Directions is null)
                {
                    DrawPanel(s.Name ?? " ", s);
                }
                else
                {
                    _IsBranchRender = true;
                    _IsBranchRender = false;
                    panelY = defaultPanelY;
                }
                contents.Add(s);
            }
        }
        private (Border, Image, ContentInfo) DrawPanel(string name, ContentInfo info, bool begin = false)
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
                Canvas.SetLeft(line, panelX);
                Canvas.SetTop(line, lineY);
                Screens.Children.Add(line);
                Screens.Width += line.Width;
                panelX += line.Width;
            }
            var grid = new Grid();
            var border = new Border()
            {
                Height = PANEL_HEIGHT,
                Width = PANEL_WIDTH,
                BorderBrush = CustomColors.FocusingScene,
            };
            var panel = new DockPanel()
            {
                Height = PANEL_HEIGHT,
                Width = PANEL_WIDTH,
                Background = Brushes.Black
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
                Source = black_image,
                Stretch = Stretch.Uniform
            };
            if (parent.CurrentContent == info)
            {
                img.Loaded += (sender, e) =>
                {
                    FocusContent(border, img, info);
                };
            }
            panel.MouseLeftButtonDown += (sender, e) =>
            {
                if (string.IsNullOrEmpty(info.Path))
                {

                }
                else if (info.IsScenes())
                {
                    if (now_content != info) FocusContent(border, img, info);
                }
                else
                {
                    MovieViewer.View(info.FullPath);
                }
            };
            var ctx = new ContextMenu();
            var item1 = new MenuItem()
            {
                Header = "エピソードを設定"
            };
            var item0 = new MenuItem()
            {
                Header = "新規"
            };
            item0.Click += (sender, e) =>
            {
                var ep = emanager.New();
                info.FullPath = ep.path;
                SetThumbnail(img, info);
                FocusContent(border, img, info);
            };
            item1.Items.Add(item0);
            ctx.Opened += (sender, e) =>
            {
                item1.Items.Clear();
                var item0 = new MenuItem()
                {
                    Header = "新規"
                };
                item0.Click += (sender, e) =>
                {
                    var ep = emanager.New();
                    info.FullPath = ep.path;
                    SetThumbnail(img, info);
                    FocusContent(border, img, info);
                };
                item1.Items.Add(item0);
                item1.Items.Add(new Separator());
                foreach (var ep in emanager.episodes)
                {
                    var item = new MenuItem()
                    {
                        Header = Utils.CutString(ep.Key, 20)
                    };
                    item.Click += (sender, e) =>
                    {
                        info.FullPath = ep.Value.path;
                        var t = ep.Value.GetThumbnail();
                        if (t is null)
                        {
                            parent.ChangeBackgroundBlack(story: false);
                        }
                        else
                        {
                            parent.Bg.Source = t;
                            parent.BgLabel.Content = Utils.CutString(System.IO.Path.GetFileName(ep.Value.smanager.scenes.First().Value.bg), 20);
                        }
                        SetThumbnail(img, info);
                        FocusContent(border, img, info);
                    };
                    item1.Items.Add(item);
                }
            };
            ctx.Items.Add(item1);
            var item2 = new MenuItem()
            {
                Header = "ムービーを設定"
            };
            item2.Click += (sender, e) =>
            {
                var res = ResourceWindow.SelectMovie(this);
                if (res is not null)
                {
                    info.FullPath = res;
                    SetThumbnail(img, info);
                }
            };
            ctx.Items.Add(item2);
            var item3 = new MenuItem()
            {
                Header = "削除"
            };
            item3.Click += (sender, e) =>
            {
                story.stories.Remove(info.id);
                Load();
            };
            ctx.Items.Add(item3);
            panel.ContextMenu = ctx;
            grid.Children.Add(img);
            var text = new Label()
            {
                Content = name,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.White,
                FontSize = 20,
                Opacity = 0.7
            };
            grid.Children.Add(text);
            border.Child = grid;
            panel.Children.Add(border);
            Canvas.SetLeft(panel, panelX);
            Canvas.SetTop(panel, panelY);
            Screens.Children.Add(panel);
            Screens.Width += PANEL_WIDTH;
            panelX += PANEL_WIDTH;
            pre_info = info;
            SetThumbnail(img, info);
            return (border, img, info);
        }
        private void SetThumbnail(Image img, ContentInfo info)
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(async () =>
                {
                    var bmp = await info.GetThumbnail();
                    img.Source = bmp is null || bmp.Width == 0 || bmp.Height == 0 ? black_image : bmp;
                });
            });
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
        private void _NewContent(object sender, RoutedEventArgs e)
        {
            NewContent();
        }
        private void NewContent()
        {
            var info = parent.story.New();
            var res = DrawPanel($"{info.id}", info);
        }
        private void FocusContent(Border border, Image img, ContentInfo info)
        {
            if (focusing_border is not null)
            {
                focusing_border.BorderThickness = new Thickness(0);
            }
            focusing_border = border;
            focusing_border.BorderThickness = border_size;
            now_content = info;
            parent.ChangeStoryContent(now_content);
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
