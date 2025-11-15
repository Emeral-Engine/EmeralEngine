using EmeralEngine.Core;
using EmeralEngine.Resource.Character;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EmeralEngine.Script
{
    /// <summary>
    /// Editor.xaml の相互作用ロジック
    /// </summary>
    public partial class ScriptEditor : Window
    {
        private const int HEADER_LENGTH_LIMIT = 10;
        private const int CHARACTER_WIDTH = 50;
        private const int CHARACTER_SPACE = CHARACTER_WIDTH + 20;
        private double DefaultWidth, DefaultHeight;
        private MainWindow parent;
        private CharacterManager cmanager;
        private bool _Handling;
        private bool IsDragged;
        private DraggingScriptInfo? draggingScriptInfo;
        private DraggingCharaInfo? draggingCharaInfo;
        private DockPanel? CurrentPanel;
        private Label? CurrentPanelHeader;

        public ObservableCollection<string> Speakers { get; set; }

        private bool IsDraggingScript
        {
            get => draggingScriptInfo is not null;
        }
        private bool IsDraggingChara
        {
            get => draggingCharaInfo is not null;
        }
        private bool EmptyScriptExists
        {
            get
            {
                foreach (var s in parent.CurrentScene.scripts)
                {
                    if (string.IsNullOrEmpty(Trim(s)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public ScriptEditor(MainWindow p)
        {
            InitializeComponent();
            DefaultWidth = Width;
            DefaultHeight = Height;
            parent = p;
            Owner = parent;
            DataContext = this;
            cmanager = new();
            Speakers = new();
            Speakers.CollectionChanged += (sender, e) =>
            {
                if (Speakers.Count == 0)
                {
                    Speaker.Text = "";
                }
                else if (Speakers.Count == 1)
                {
                    Speaker.Text = Speakers.First();
                }
            };
        }
        public void FocusTextBox()
        {
            Script.Focus();
        }

        public void LoadLog()
        {
            LogGrid.Children.Clear();
            LogGrid.RowDefinitions.Clear();
            if (parent.CurrentScene.scripts.Count == 0)
            {
                parent.CurrentScene.AddScript();
                CreateLogPanel(new ScriptInfo());
                Script.Text = "";
            }
            else
            {
                for (var i = 0; i < parent.CurrentScene.scripts.Count; i++)
                {
                    var p = CreateLogPanel(parent.CurrentScene.scripts[i]);
                    if (i == parent.CurrentScriptIndex)
                    {
                        p.BringIntoView();
                    }
                }
                if (parent.CurrentScriptIndex < 0)
                {
                    parent.CurrentScriptIndex = parent.CurrentScene.scripts.Count - 1;
                }
            }
            _Handling = true;
            Script.Text = Trim(parent.CurrentScript.script);
            Speaker.Text = parent.CurrentScript.speaker;
            Memo.Text = parent.CurrentScene.memo;
            _Handling = false;
            DeleteSctiptButton.IsEnabled = parent.CurrentScene.scripts.Count > 1;
            NewScriptButton.IsEnabled = MainWindow.pmanager.Project.EditorSettings.AddScriptWhenEmpty || !EmptyScriptExists;
            FocusHeader(CurrentPanelHeader);
            LoadCharacterPictures();
            parent.LoadPreview(bg: false);
        }

        private DockPanel CreateLogPanel(ScriptInfo s)
        {
            var grid = new Grid();
            var sep = new Separator()
            {
                VerticalAlignment = VerticalAlignment.Top
            };
            LogGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(5, GridUnitType.Pixel)
            });
            Grid.SetRow(sep, LogGrid.Children.Count);
            LogGrid.Children.Add(sep);
            var panel = new DockPanel()
            {
                Background = CustomColors.FocusingScript,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 200,
                Height = 100,
                LastChildFill = false,
            };
            panel.MouseEnter += (sender, e) =>
            {
                if (panel.Opacity != 0.5) panel.Opacity = 0.7;
                if (IsDraggingScript)
                {
                    var r = Grid.GetRow(panel);
                    var to_index = (r - 1) / 2;
                    if (draggingScriptInfo.current_index != to_index)
                    {
                        draggingScriptInfo.to_index = to_index;
                        var is_down = draggingScriptInfo.current_index < draggingScriptInfo.to_index;
                        Grid.SetRow(draggingScriptInfo.drag_source, r);
                        Grid.SetRow(panel, is_down ? r - 2 : r + 2);
                        Utils.Swap(parent.CurrentScene.scripts, draggingScriptInfo.current_index, draggingScriptInfo.to_index);
                        draggingScriptInfo.current_index = draggingScriptInfo.to_index;
                        parent.CurrentScriptIndex = to_index;
                    }
                }
            };
            panel.MouseLeave += (sender, e) =>
            {
                if (panel.Opacity != 0.5) panel.Opacity = 1;
            };
            var header = new Label()
            {
                Content = Utils.CutString(Handle(s), HEADER_LENGTH_LIMIT),
                FontSize = 20,
                Foreground = Brushes.Black
            };
            grid.Children.Add(header);
            panel.MouseLeftButtonDown += (sender, e) =>
            {
                if (!IsDraggingScript)
                {
                    parent.CurrentScriptIndex = (Grid.GetRow(panel) - 1) / 2;
                    _Handling = true;
                    Script.Text = Trim(parent.CurrentScript.script);
                    Speaker.Text = parent.CurrentScript.speaker;
                    _Handling = false;
                    var for_drag_grid = new Grid();
                    var drag_target = new DockPanel()
                    {
                        Background = CustomColors.DraggingScript,
                        VerticalAlignment = VerticalAlignment.Top,
                        Width = 200,
                        Height = 100,
                        LastChildFill = false,
                        IsHitTestVisible = false,
                        Visibility = Visibility.Hidden
                    };
                    for_drag_grid.Children.Add(new Label()
                    {
                        Content = Utils.CutString(s.script, HEADER_LENGTH_LIMIT),
                        FontSize = 20,
                    });
                    drag_target.Children.Add(for_drag_grid);
                    ViewerCanvas.Children.Add(drag_target);
                    draggingScriptInfo = new()
                    {
                        drag_target = drag_target,
                        drag_source = panel,
                        from_index = (Grid.GetRow(panel) - 1) / 2
                    };
                    draggingScriptInfo.current_index = draggingScriptInfo.from_index;
                    FocusPanel(panel, header);
                    LoadCharacterPictures();
                }
            };
            panel.Children.Add(grid);
            LogGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(100)
            });
            if (parent.CurrentScriptIndex < 0 || LogGrid.Children.Count / 2 == parent.CurrentScriptIndex)
            {
                CurrentPanel = panel;
                CurrentPanelHeader = header;
            }
            Grid.SetRow(panel, LogGrid.Children.Count);
            LogGrid.Children.Add(panel);
            return panel;
        }

        public string Handle(string s)
        {
            if (string.IsNullOrEmpty(Speaker.Text))
            {
                return s;
            }
            else
            {
                return $"「{Trim(s)}」";
            }
        }

        private string Handle(ScriptInfo s)
        {
            if (string.IsNullOrEmpty(s.speaker))
            {
                return s.script;
            }
            else
            {
                return $"「{Trim(s.script)}」";
            }
        }
        private string Trim(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            else
            {
                return s.TrimStart('「').TrimEnd('」');
            }
        }

        private string Trim(ScriptInfo s)
        {
            if (string.IsNullOrEmpty(s.speaker))
            {
                return s.script;
            }
            else
            {
                return s.script.TrimStart('「').TrimEnd('」');
            }
        }
        private void FocusPanel(DockPanel panel, Label header)
        {
            if (CurrentPanelHeader is not null)
            {
                ReleasePanel();
            }
            CurrentPanel = panel;
            CurrentPanelHeader = header;
            CurrentPanel.Opacity = 0.5;
            FocusTextBox();
            parent.LoadPreview(bg: false);
        }
        private void FocusHeader(Label header)
        {
            if (CurrentPanelHeader is not null)
            {
                ReleasePanel();
            }
            CurrentPanelHeader = header;
            CurrentPanel.Opacity = 0.5;
        }
        private void ReleasePanel()
        {
            CurrentPanel.Opacity = 1;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var pre_charas = parent.CurrentScript.charas;
            var pre_speaker = parent.CurrentScript.speaker;
            parent.CurrentScriptIndex++;
            parent.CurrentScene.AddScript(parent.CurrentScriptIndex);
            Script.Text = "";
            parent.CurrentScript.charas = new(pre_charas);
            parent.CurrentScript.speaker = new(pre_speaker);
            LoadLog();
            Script.Focus();
            DeleteSctiptButton.IsEnabled = true;
            NewScriptButton.IsEnabled = MainWindow.pmanager.Project.EditorSettings.AddScriptWhenEmpty;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_Handling) return;
            var s = Trim(Script.Text);
            if (Script.Text == s)
            {
                parent.CurrentScript.script = Handle(Script.Text);
                CurrentPanelHeader.Content = Utils.CutString(parent.CurrentScript.script, HEADER_LENGTH_LIMIT);
                NewScriptButton.IsEnabled = MainWindow.pmanager.Project.EditorSettings.AddScriptWhenEmpty || !EmptyScriptExists;
                parent.Script.Text = parent.CurrentScript.script;
            }
            else
            {
                Script.Text = s;
            }

        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scale.ScaleX = ActualWidth / DefaultWidth;
            Scale.ScaleY = ActualHeight / DefaultHeight;
        }

        private void Memo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_Handling) return;
            parent.CurrentScene.memo = Memo.Text;
        }

        private void OnLogViewerMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsDraggingScript)
            {
                IsDragged = true;
                draggingScriptInfo.drag_source.Visibility = Visibility.Hidden;
                draggingScriptInfo.drag_target.Visibility = Visibility.Visible;
                var pos = e.GetPosition(LogViewer);
                Canvas.SetLeft(draggingScriptInfo.drag_target, 0);
                Canvas.SetTop(draggingScriptInfo.drag_target, pos.Y - draggingScriptInfo.drag_target.ActualHeight / 2);
                if (0 <= pos.Y && pos.Y <= 30)
                {
                    LogViewer.ScrollToVerticalOffset(LogViewer.VerticalOffset - 10);
                }
                else if (LogViewer.ActualHeight - 30 <= pos.Y && pos.Y <= LogViewer.ActualHeight)
                {
                    LogViewer.ScrollToVerticalOffset(LogViewer.VerticalOffset + 10);
                }
            }
        }

        private void OnLogViewerMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsDraggingScript && IsDragged)
            {
                ViewerCanvas.Children.Clear();
                draggingScriptInfo.drag_source.Visibility = Visibility.Visible;
            }
            draggingScriptInfo = null;
            IsDragged = false;
        }

        private void OnViewerMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsDraggingScript && IsDragged)
            {
                ViewerCanvas.Children.Remove(draggingScriptInfo.drag_target);
                draggingScriptInfo.drag_source.Visibility = Visibility.Visible;
            }
            draggingScriptInfo = null;
            IsDragged = false;
        }

        private void LoadCharacterPictures()
        {
            var speaker = Speaker.Text;
            CharacterPictures.Children.Clear();
            Speakers.Clear();
            CharacterPictures.Width = 0;
            foreach (var c in parent.CurrentScript.charas)
            {
                if (!string.IsNullOrWhiteSpace(c))
                {
                    DrawCharacterPicturePanel(cmanager.Combine(c));
                    Speakers.Add(Path.GetDirectoryName(c));
                }
            }
            Speaker.Text = speaker;
        }

        private void AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            var pic = CharacterWindow.SelectCharacterPicture(this);
            if (!string.IsNullOrEmpty(pic))
            {
                DrawCharacterPicturePanel(pic);
                var rel = cmanager.Format(pic);
                parent.CurrentScript.charas.Add(rel);
                Speakers.Add(Path.GetDirectoryName(rel));
                parent.LoadPreview(script: false, msw: false, bg: false);
            }
        }

        private void DrawCharacterPicturePanel(string path)
        {
            var chara = Path.GetDirectoryName(cmanager.Format(path));
            var panel = new DockPanel()
            {
                Width = CHARACTER_WIDTH,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            panel.MouseEnter += (sender, e) =>
            {
                if (IsDraggingChara)
                {
                    var idx = Grid.GetColumn(panel);
                    Grid.SetColumn(panel, draggingCharaInfo.current_index);
                    Grid.SetColumn(draggingCharaInfo.drag_source, idx);
                    draggingCharaInfo.current_index = idx;
                }
                else
                {
                    panel.Opacity = 0.5;
                }
            };
            panel.MouseLeave += (sender, e) =>
            {
                panel.Opacity = 1;
            };
            var img = new Image()
            {
                Stretch = Stretch.Uniform,
            };
            panel.MouseLeftButtonDown += (sender, e) =>
            {
                var idx = Grid.GetColumn(panel);
                var target = new DockPanel()
                {
                    Width = CHARACTER_WIDTH,
                    IsHitTestVisible = false,
                    Visibility = Visibility.Hidden
                };
                var i = new Image()
                {
                    Stretch = Stretch.Uniform,
                    IsHitTestVisible = false,
                    Source = Utils.CreateBmp(path)
                };
                target.Children.Add(i);
                draggingCharaInfo = new DraggingCharaInfo()
                {
                    current_index = idx,
                    from_index = idx,
                    drag_target = target,
                    drag_source = panel
                };
                CharacterPictureDrag.Children.Add(target);
                IsDragged = false;
            };
            var menu = new ContextMenu();
            var item = new MenuItem()
            {
                Header = "変更"
            };
            item.Click += (sender, e) =>
            {
                var idx = Grid.GetColumn(panel);
                var new_chara = CharacterWindow.SelectCharacterPicture(this);
                if (!string.IsNullOrEmpty(new_chara))
                {
                    Task.Run(() =>
                    {
                        var bmp = Utils.CreateBmp(new_chara);
                        Dispatcher.Invoke(() =>
                        {
                            img.Source = bmp;
                        });
                    });
                    parent.CurrentScript.charas[idx] = cmanager.Format(new_chara);
                    var new_chara_name = Path.GetDirectoryName(parent.CurrentScript.charas[idx]);
                    if (Speaker.Text == chara)
                    {
                        Speakers.Remove(chara);
                        Speakers.Add(new_chara_name);
                        Speaker.Text = new_chara_name;
                        chara = new_chara_name;
                    }
                    parent.LoadPreview(script: false, msw: false, bg: false);
                }
            };
            menu.Items.Add(item);
            var item2 = new MenuItem()
            {
                Header = "削除"
            };
            item2.Click += (sender, e) =>
            {
                parent.CurrentScript.charas.RemoveAt(Grid.GetColumn(panel));
                Speakers.Remove(chara);
                LoadCharacterPictures();
                parent.LoadPreview(script: false, msw: false, bg: false);
            };
            menu.Items.Add(item2);
            panel.ContextMenu = menu;
            Task.Run(() =>
            {
                var bmp = Utils.CreateBmp(path);
                Dispatcher.Invoke(() =>
                {
                    img.Source = bmp;
                });
            });
            panel.Children.Add(img);
            CharacterPictures.Width += CHARACTER_SPACE;
            CharacterPictures.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(CHARACTER_SPACE)
            });
            Grid.SetColumn(panel, CharacterPictures.Children.Count);
            CharacterPictures.Children.Add(panel);
        }

        private void DeleteScriptButton_Click(object sender, RoutedEventArgs e)
        {
            parent.CurrentScene.scripts.RemoveAt(parent.CurrentScriptIndex);
            if (parent.CurrentScriptIndex != 0)
            {
                parent.CurrentScriptIndex--;
            }
            LoadLog();
        }

        private void OnCharaViewerMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDraggingChara)
            {
                IsDragged = true;
                draggingCharaInfo.drag_source.Visibility = Visibility.Hidden;
                draggingCharaInfo.drag_target.Visibility = Visibility.Visible;
                var pos = e.GetPosition(CharactersViewer);
                Canvas.SetTop(draggingCharaInfo.drag_target, pos.Y - (CharactersViewer.ActualHeight / 2));
                Canvas.SetLeft(draggingCharaInfo.drag_target, pos.X - draggingCharaInfo.drag_target.ActualWidth / 2);
                if (0 <= pos.X && pos.X <= 30)
                {
                    CharactersViewer.ScrollToVerticalOffset(LogViewer.HorizontalOffset - 10);
                }
                else if (CharactersViewer.ActualWidth - 30 <= pos.X && pos.X <= CharactersViewer.ActualWidth)
                {
                    CharactersViewer.ScrollToVerticalOffset(CharactersViewer.HorizontalOffset + 10);
                }
            }
        }

        private void OnCharaMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsDraggingChara)
            {
                EndDragChara();
            }
        }

        private void OnCharaMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDraggingChara)
            {
                EndDragChara();
            }
        }

        private void EndDragChara()
        {
            draggingCharaInfo.drag_source.Visibility = Visibility.Visible;
            CharacterPictureDrag.Children.Clear();
            Utils.Swap(parent.CurrentScript.charas, draggingCharaInfo.from_index, draggingCharaInfo.current_index);
            draggingCharaInfo = null;
            parent.LoadPreview(script: false, msw: false, bg: false);
        }

        private void Speaker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_Handling) return;
            if (parent.CurrentScript.speaker == "" || Speaker.Text == "")
            {
                parent.CurrentScript.script = Handle(Script.Text);
                CurrentPanelHeader.Content = Utils.CutString(Handle(Script.Text), HEADER_LENGTH_LIMIT);
            }
            parent.CurrentScript.speaker = Speaker.Text;
            parent.LoadPreview(script: false, charas: false, bg: false);
        }

        private void OnCharactersMouseWheel(object sender, MouseWheelEventArgs e)
        {
            CharactersViewer.ScrollToHorizontalOffset(CharactersViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }

    class DraggingScriptInfo
    {
        public DockPanel drag_target;
        public DockPanel drag_source;
        public int from_index = -1;
        public int current_index = -1;
        public int to_index = -1;
    }

    class DraggingCharaInfo
    {
        public DockPanel drag_target;
        public DockPanel drag_source;
        public int from_index = -1;
        public int current_index = -1;
        public int to_index = -1;
    }
}
