using EmeralEngine.Core;
using EmeralEngine.Resource;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace EmeralEngine.TitleScreen
{
    public partial class TitleScreenDesigner : Window
    {
        private static Thickness SELECTED_THICK = new Thickness(3);
        private static Thickness ZERO_THICK = new Thickness(0);
        private const int RESIZE_HIT_THICK = 30;
        private const int DEFAULT_PREVIEW_WIDTH = 600;
        private const int NORMAL_WIDTH = 900;
        private const int NORMAL_HEIGHT = 450;
        private const int MIN_SIZE = 10;
        private string[] BUTTONS_NAME = new string[] { "StartButton", "LoadButton", "FinishButton"};
        private double ScaleRate;
        private int _currentType;
        private Image BackgroundImage;
        public DesignerElementManager ElementManager;
        public Dictionary<int, int> ButtonFuncs;
        private CustomTitlePage CustomTitlePage;
        private NormalTitlePage NormalTitlePage;
        public int SelectingButton;
        private Border _selectingContent;
        public bool IsHandling;
        private Border _ButtonsBorder;

        public TitleScreenDesigner(MainWindow w)
        {
            InitializeComponent();
            Owner = w;
            Loaded += (sender, e) =>
            {
                _currentType = ButtonType.SelectedIndex;
            };
            ButtonFuncs = new();
            CustomTitlePage = new(this);
            NormalTitlePage = new(this);
            ButtonType.SelectedIndex = 1;
            ButtonFrame.Navigate(CustomTitlePage);
            ElementManager = new();
            LoadXaml();
            AdjustPreview();
        }

        private void LoadXaml()
        {
            PreviewArea.Children.Clear();
            var canvas = (Canvas)XamlReader.Parse(MainWindow.pmanager.ReadTitleScreenXaml(MainWindow.pmanager.ProjectResourceDir));
            var children = canvas.Children.Cast<UIElement>().ToList();
            canvas.Children.Clear();
            foreach (FrameworkElement element in children)
            {
                if (element.Name == "BackgroundImage")
                {
                    PreviewArea.Children.Add(element);
                    BackgroundImage = element as Image;
                }
                else
                {
                    var border = DragResizeHelper.Make(PreviewArea, element);
                    if (BUTTONS_NAME.Contains(element.Name))
                    {
                        var h = border.GetHashCode();
                        ButtonFuncs.Add(h, ButtonTypes.Get(element.Name));
                        CustomTitlePage.Buttons.Add(border);
                        border.PreviewMouseLeftButtonDown += (sender, e) =>
                        {
                            CustomTitlePage.ButtonFunc.IsEnabled = true;
                            ElementManager.Focus(border);
                            CustomTitlePage.SelectingButton = h;
                            CustomTitlePage.LimitButtonTypeSelection();
                            CustomTitlePage.ButtonFunc.SelectedIndex = CustomTitlePage.ButtonSelections.IndexOf(ButtonTypes.Get(ButtonFuncs[CustomTitlePage.SelectingButton]));
                        };
                    }
                    else
                    {
                        border.PreviewMouseLeftButtonDown += (sender, e) =>
                        {
                            CustomTitlePage.ButtonFunc.SelectedIndex = -1;
                            CustomTitlePage.ButtonFunc.IsEnabled = false;
                            ElementManager.Focus(border);
                        };
                    }
                    PreviewArea.Children.Add(border);
                    if (element.Name == "ButtonsBorder")
                    {
                        _ButtonsBorder = border;
                        IsHandling = true;
                        ButtonType.SelectedIndex = 0;
                        IsHandling = false;
                        ButtonFrame.Navigate(NormalTitlePage);
                    }
                    else
                    {
                        MakeContextMenu(border);
                    }
                }
            }
        }

        private void AdjustPreview()
        {
            PreviewArea.Width = MainWindow.pmanager.Project.Size[0];
            PreviewArea.Height = MainWindow.pmanager.Project.Size[1];
            ScaleRate = Math.Min( DEFAULT_PREVIEW_WIDTH / PreviewArea.ActualWidth, Height / PreviewArea.ActualHeight );
            PreviewScale.ScaleX = ScaleRate;
            PreviewScale.ScaleY = ScaleRate;
        }

        private void SelectBackground_Click(object sender, RoutedEventArgs e)
        {
            var res = ResourceWindow.SelectImage(this);
            if (!string.IsNullOrEmpty(res))
            {
                BackgroundImage.Source = Utils.CreateBmp(res);
            }
        }

        public void MakeContextMenu(Border b)
        {
            var menu = new ContextMenu();
            var item1 = new MenuItem()
            {
                Header = "削除"
            };
            item1.Click += (sender, e) =>
            {
                PreviewArea.Children.Remove(b);
            };
            menu.Items.Add(item1);
            b.ContextMenu = menu;
            b.ContextMenuOpening += (sender, e) =>
            {
                if (_selectingContent is not null)
                {
                    _selectingContent.BorderThickness = ZERO_THICK;
                }
                _selectingContent = b;
                _selectingContent.BorderThickness = SELECTED_THICK;
            };
        }

        private void ExportXaml_Click(object sender, RoutedEventArgs e)
        {
            var xaml = new StringBuilder();
            xaml.AppendLine("""
                <Canvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        Background="Transparent"
                        Name="TitleScreen">
                """);
            var buttons = "";
            var isCustom = ButtonType.SelectedIndex == 1;
            foreach (UIElement element in PreviewArea.Children)
            {
                if (element is Image img)
                {
                    var path = ImageUtils.GetFileName(img.Source);
                    if (path == "")
                    {
                        xaml.AppendLine($"    <Image Name=\"BackgroundImage\" Canvas.ZIndex=\"0\" Width=\"{{Binding Width, ElementName=TitleScreen}}\" Height=\"{{Binding Height, ElementName=TitleScreen}}\" Stretch=\"Fill\" />");
                    }
                    else
                    {
                        xaml.AppendLine($"    <Image Name=\"BackgroundImage\" Canvas.ZIndex=\"0\" Width=\"{{Binding Width, ElementName=TitleScreen}}\" Height=\"{{Binding Height, ElementName=TitleScreen}}\" Source=\"{path}\" Stretch=\"Fill\" />");
                    }
                }
                else if (element == _ButtonsBorder)
                {
                    var border = element as ResizableBorder;
                    var canvas = border.Child as Canvas;
                    buttons = $"""
                            <Border Canvas.ZIndex="1" Name="ButtonsBorder" Visibility="Visible" Width="{canvas.Width}" Height="{canvas.Height}" Canvas.Left="{Canvas.GetLeft(border)}" Canvas.Top="{Canvas.GetTop(border)}" BorderBrush="White" BorderThickness="3">
                                <Grid>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="*"/>
                                        <RowDefinition Height="*"/>
                                        <RowDefinition Height="*"/>
                                    </Grid.RowDefinitions>
                                    <Button Name="StartButton">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <ContentPresenter/>
                                            </ControlTemplate>
                                        </Button.Template>
                                        <Grid>
                                            <Label Content="スタート" FontWeight="Bold" FontSize="40" HorizontalAlignment="Center" Foreground="White"/>
                                            <Rectangle Fill="Gray" Opacity="0.3"/>
                                        </Grid>
                                    </Button>
                                    <Button Grid.Row="1" Name="LoadButton">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <ContentPresenter/>
                                            </ControlTemplate>
                                        </Button.Template>
                                        <Grid>
                                            <Label Content="途中から" FontWeight="Bold" FontSize="40" HorizontalAlignment="Center" Foreground="White"/>
                                            <Rectangle Fill="Gray" Opacity="0.3"/>
                                        </Grid>
                                    </Button>
                                    <Button Grid.Row="2" Name="FinishButton">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <ContentPresenter/>
                                            </ControlTemplate>
                                        </Button.Template>
                                        <Grid>
                                            <Label Content="終了" FontWeight="Bold" FontSize="40" HorizontalAlignment="Center" Foreground="White"/>
                                            <Rectangle Fill="Gray" Opacity="0.3"/>
                                        </Grid>
                                    </Button>
                                </Grid>
                            </Border>
                            """;
                }
                else if (element is ResizableBorder border)
                {
                    UIElement? target = null;
                    foreach (var c in (border.Child as Canvas).Children)
                    {
                        if (c is not Thumb v)
                        {
                            target = c as UIElement;
                        }
                    }
                    if (target is Button b)
                    {
                        var h = border.GetHashCode();
                        var func = ButtonFuncs[h] switch
                        {
                            ButtonTypes.START => "StartButton",
                            ButtonTypes.LOAD => "LoadButton",
                            ButtonTypes.END => "FinishButton"
                        };
                        var bg = b.Content as Image;
                        string path;
                        if (bg.Source is BitmapImage bmp)
                        {
                            path = bmp.UriSource.LocalPath;
                        }
                        else
                        {
                            path = ((BitmapFrame)bg.Source).Decoder.ToString();
                        }
                        xaml.Append($"""
                                <Button Name="{func}" Canvas.ZIndex="1" Canvas.Left="{Canvas.GetLeft(border)}" Canvas.Top="{Canvas.GetTop(border)}" Width="{border.ActualWidth}" Height="{border.ActualHeight}" Background="Transparent" BorderBrush="Transparent">
                                    <Button.Template>
                                        <ControlTemplate TargetType="Button">
                                            <ContentPresenter/>
                                        </ControlTemplate>
                                    </Button.Template>
                                    <Image Source="{System.IO.Path.GetFileName(path)}"/>
                                </Button>
                                """);
                    }else if (target is Image i)
                    {
                        xaml.AppendLine($"    <Image Canvas.ZIndex=\"1\" Canvas.Left=\"{Canvas.GetLeft(border)}\" Canvas.Top=\"{Canvas.GetTop(border)}\" Width=\"{border.ActualWidth}\" Height=\"{border.ActualHeight}\" Source=\"{ImageUtils.GetFileName(i.Source)}\"/>");
                    }
                }
            }
            xaml.Append($"    {buttons}\n</Canvas>");
            File.WriteAllText(MainWindow.pmanager.ProjectTitleScreen, xaml.ToString());
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustPreview();
            ScaleRate = Math.Min(ActualWidth / NORMAL_WIDTH, ActualHeight / NORMAL_HEIGHT);
            Scale.ScaleX = ScaleRate;
            Scale.ScaleY = ScaleRate;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var res = ResourceWindow.SelectImage(this);
            if (!string.IsNullOrEmpty(res))
            {
                var img = new Image()
                {
                    Source = Utils.CreateBmp(res)
                };
                var border = DragResizeHelper.Make(PreviewArea, img);
                border.PreviewMouseLeftButtonDown += (sender, e) =>
                {
                    CustomTitlePage.ButtonFunc.SelectedIndex = -1;
                    CustomTitlePage.ButtonFunc.IsEnabled = false;
                    ElementManager.Focus(border);
                };
                MakeContextMenu(border);
                Canvas.SetLeft(border, 0);
                Canvas.SetTop(border, 0);
                Canvas.SetZIndex(border, 0);
                PreviewArea.Children.Add(border);
            }
        }

        private void ButtonType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || IsHandling) return;
            var res = MessageBox.Show("切り替えると現在のボタンに関する設定は全て失われます\n本当に切り替えますか？", MainWindow.CAPTION, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.No)
            {
                IsHandling = true;
                ButtonType.SelectedIndex = _currentType;
                IsHandling = false;
                return;
            }
            _currentType = ButtonType.SelectedIndex;
            switch (_currentType)
            {
                case 0:
                    foreach (var b in PreviewArea.Children.Cast<UIElement>().ToList())
                    {
                        if (ButtonFuncs.ContainsKey(b.GetHashCode()))
                        {
                            PreviewArea.Children.Remove(b);
                        }
                    }
                    CustomTitlePage.Buttons.Clear();
                    ButtonFuncs.Clear();
                    ButtonFrame.Navigate(NormalTitlePage);
                    var border = XamlReader.Parse(MainWindow.pmanager.GetNormalButtons()) as Border;
                    _ButtonsBorder = DragResizeHelper.Make(PreviewArea, border);
                    _ButtonsBorder.PreviewMouseLeftButtonDown += (sender, e) =>
                    {
                        CustomTitlePage.ButtonFunc.SelectedIndex = -1;
                        CustomTitlePage.ButtonFunc.IsEnabled = false;
                        ElementManager.Focus(_ButtonsBorder as ResizableBorder);
                    };
                    PreviewArea.Children.Add(_ButtonsBorder);
                    break;
                case 1:
                    PreviewArea.Children.Remove(_ButtonsBorder);
                    ButtonFrame.Navigate(CustomTitlePage);
                    break;
            }
        }
    }
}
