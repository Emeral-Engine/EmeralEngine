using EmeralEngine.Builder;
using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using EmeralEngine.Notify;
using EmeralEngine.Project;
using EmeralEngine.Resource;
using EmeralEngine.Resource.Character;
using EmeralEngine.Scene;
using EmeralEngine.Script;
using EmeralEngine.Setting;
using EmeralEngine.Story;
using EmeralEngine.TitleScreen;
using Microsoft.CodeAnalysis;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EmeralEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string CAPTION = "EmeralEngine v0.3";
        private const double DEFAULT_WIDTH = 800;
        private const double DEFAULT_HEIGHT = 450;
        private double PREVIEW_DEFAULT_WIDTH = 600;
        private double PREVIEW_MAX_DEFAULT_WIDTH = 780;
        public int CurrentScriptIndex;
        public static ProjectManager pmanager = new();
        public Logger Log;
        public MessageWindowManager mmanager;
        public StoryManager story;
        public EpisodeManager emanager;
        private BackupManager bmanager;
        public Managers Managers;
        private CharacterWindow _CharacterWindow;
        private ScriptEditor _ScriptWindow;
        private SceneWindow _SceneWindow;
        private StoryEditor _StoryEditor;
        public ResourceWindow ResourceWindow;
        private MessageWindowDesigner _MessageWindowDesigner;
        private TitleScreenDesigner _TitleScreenDesigner;
        private SettingWindow _SettingWindow;
        public SceneInfo CurrentScene;
        public ContentInfo CurrentContent;
        public EpisodeInfo CurrentEpisode;
        private Assembly[] references;
        private DispatcherTimer backup_timer;
        private DispatcherTimer updateProgressTimer;
        private bool IsCreated;

        public ScriptInfo CurrentScript
        {
            get
            {
                if (CurrentScene.scripts.Count <= CurrentScriptIndex)
                {
                    CurrentScriptIndex = CurrentScene.scripts.Count - 1;
                    return CurrentScene.scripts.Last();
                }
                else
                {
                    return CurrentScene.scripts[CurrentScriptIndex];
                }
            }
        }
        private RenderTargetBitmap black_image
        {
            get
            {
                var rect = new System.Windows.Shapes.Rectangle()
                {
                    Width = Preview.ActualWidth,
                    Height = Preview.ActualHeight,
                    Fill = Brushes.Black
                };
                var size = new Size(100, 200);
                rect.Measure(size);
                rect.Arrange(new Rect(size));
                var b = new RenderTargetBitmap(100, 200, 96, 96, PixelFormats.Pbgra32);
                b.Render(rect);
                return b;
            }
        }
        private bool IsProjectOpened
        {
            get => !string.IsNullOrEmpty(pmanager.ProjectName);
        }
        public MainWindow(bool Default=true)
        {
            references = AppDomain.CurrentDomain.GetAssemblies();
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                var r = Math.Min(ActualWidth / DEFAULT_WIDTH, ActualHeight / DEFAULT_HEIGHT);
                Scale.ScaleX = r;
                Scale.ScaleY = r;
                if (Default)
                {
                    OpenSelectedProject();
                }
            };
            Closing += (sender, e) =>
            {
                e.Cancel = AskSave();
            };
            /*
            App.Current.DispatcherUnhandledException += (sender, e) => {
                Dispatcher.Invoke(() =>
                {
                    ErrorNotifyWindow.Show(e.Exception.Message);
                    AskSave();
                });
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ErrorNotifyWindow.Show((e.ExceptionObject as Exception).Message);
                    AskSave();
                });
                Environment.Exit(1);
            };
            Application.Current.Exit += (sender, e) =>
            {
                if (pmanager.Temp is not null)
                {
                    pmanager.Temp.Dispose();
                }
            };
            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Dispatcher.Invoke(() =>
                {
                    ErrorNotifyWindow.Show(e.Exception.Message);
                });
            };
            */
            backup_timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            backup_timer.Tick += (sender, e) =>
            {
                if (bmanager is not null)
                {
                    Task.Run(bmanager.Backup);
                }
            };
            Title = CAPTION;
        }

        private bool AskSave()
        {
            if (!IsProjectOpened) return false;
            var res = MessageBox.Show("終了する前にプロジェクトを保存しますか？", MainWindow.CAPTION, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            switch (res)
            {
                case MessageBoxResult.Yes:
                    Save(false);
                    break;
                case MessageBoxResult.No:
                    break;
                default:
                    return true;
            }
            return false;
        }
        private void Refresh()
        {
            var frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        public void NewProject(string title, int[] size)
        {
            pmanager.NewProject(title, size);
            IsCreated = true;
        }

        public void LoadProject(string path)
        {
            CloseSubWindows();
            pmanager.LoadProject(path);
            Title = $"{CAPTION} {pmanager.ProjectName} ロード中...";
            Refresh();
            Debug.WriteLine(pmanager.Temp.path);
            backup_timer.Stop();
            CurrentScriptIndex = -1;
            mmanager = new();
            story = new();
            emanager = new();
            Managers = new()
            {
                ProjectManager = pmanager,
                StoryManager = story,
                EpisodeManager = emanager,
                MessageWindowManager = new MessageWindowManager(),
            };
            bmanager = new(Managers);
            Managers.BackupManager = bmanager;
            Log = new(Managers);
            if (emanager.episodes.Count == 0)
            {
                CurrentEpisode = emanager.New();
            }
            else
            {
                CurrentEpisode = emanager.episodes.Values.First();
            }
            if (story.stories.Count == 0)
            {
                CurrentContent = story.New(CurrentEpisode.path);
                CurrentContent.Path = CurrentEpisode.path;
                pmanager.Project.Story.Add(CurrentContent);
                pmanager.SaveProject();
            }
            else
            {
                CurrentContent = story.stories.First().Value;
            }
            CurrentScene = CurrentEpisode.smanager.scenes.First().Value;
            if (0 < CurrentScene.bg.Length) ChangeBackground(pmanager.GetResource(CurrentScene.bg));
            else ChangeBackgroundBlack();
            BgLabel.Content = Utils.CutString(CurrentScene.bg, 8, lines: 2);
            BgmLabel.Content = CurrentScene.bgm;
            if (pmanager.Project.Startup.Story) OpenStoryEditor();
            if (pmanager.Project.Startup.Scene) OpenSceneEditor();
            if (pmanager.Project.Startup.Script) OpenScriptEditor();
            if (pmanager.Project.Startup.Resource) OpenResourceManager();
            if (pmanager.Project.Startup.Chara) OpenCharacterManager();
            if (pmanager.Project.Startup.Msw) OpenMessageDesigner();
            AdjustPreviewSize();
            LoadPreview();
            Title = $"{CAPTION} {pmanager.ProjectName}";
            Activate();
            backup_timer.Start();
        }

        private void AdjustPreviewSize()
        {
            if (IsProjectOpened)
            {
                var w = pmanager.Project.Size[0];
                var h = pmanager.Project.Size[1];
                double r;
                if (ToolsExpander.IsExpanded)
                {
                    r = Math.Min(PREVIEW_DEFAULT_WIDTH / w, ToolsExpander.ActualHeight / h);
                }
                else
                {
                    r = Math.Min(PREVIEW_MAX_DEFAULT_WIDTH / w, ToolsExpander.ActualHeight / h);
                }
                Preview.Width = w * r;
                Preview.Height = h * r;
                PreviewScale.ScaleX = r;
                PreviewScale.ScaleY = r;
            }
        }

        public void ReLoad(bool script=true, bool scene=true, bool story=true)
        {
            if (script && IsAvailable(_ScriptWindow))
            {
                _ScriptWindow.LoadLog();
            }
            if (scene && IsAvailable(_SceneWindow))
            {
                _SceneWindow.Load();
            }
            if (story && IsAvailable(_StoryEditor))
            {
                _StoryEditor.Load();
            }
        }

        public void LoadPreview(bool script=true, bool msw=true, bool charas=true, bool bg=true)
        {
            var scriptIsAvailable = IsAvailable(_ScriptWindow);
            if (script)
            {
                if (scriptIsAvailable)
                {
                    Script.Text = CurrentScript?.script;
                }
                else
                {
                    CurrentScriptIndex = 0;
                    Script.Text = CurrentScene.scripts[0].script;
                }
            }
            if (msw)
            {
                var window = mmanager[CurrentScene.msw];
                MessageWindow.Width = window.WindowContents.Width;
                MessageWindow.Height = window.WindowContents.Height;
                MessageWindowBg.Width = window.WindowContents.Width;
                MessageWindowBg.Height = window.WindowContents.Height;
                MessageWindow.Background = window.WindowContents.Background;
                MessageWindow.Background.Opacity = window.WindowContents.Background.Opacity;
                MessageWindowBg.Opacity = window.MessageWindowBg.Opacity;
                if (window.MessageWindowBg.Source is null)
                {
                    MessageWindowBg.Source = null;
                }
                else
                {
                    MessageWindowBg.Source = Utils.CreateBmp(ImageUtils.GetFilePath(window.MessageWindowBg.Source));
                }
                Canvas.SetLeft(Script, Canvas.GetLeft(window.Script));
                Canvas.SetTop(Script, Canvas.GetTop(window.Script));
                Script.Width = window.Script.Width;
                Script.FontSize = window.Script.FontSize;
                Script.FontFamily = window.Script.FontFamily;
                Script.Foreground = window.Script.Foreground;
                if (string.IsNullOrWhiteSpace(CurrentScript.speaker))
                {
                    if (scriptIsAvailable) NamePlate.Visibility = Visibility.Hidden;
                }
                else
                {
                    NamePlate.Visibility = Visibility.Visible;
                    Canvas.SetLeft(NamePlate, Canvas.GetLeft(window.NamePlate));
                    Canvas.SetTop(NamePlate, Canvas.GetTop(window.NamePlate));
                    NamePlate.Width = window.NamePlate.Width;
                    NamePlate.Height = window.NamePlate.Height;
                    NamePlate.Background = window.NamePlate.Background;
                    NamePlate.Background.Opacity = window.NamePlate.Background.Opacity;
                    if (window.NamePlateBg.Source is not null)
                    {
                        NamePlateBgImage.Source = Utils.CreateBmp(ImageUtils.GetFilePath(window.NamePlateBg.Source));
                    }
                    NamePlateBgImage.Opacity = window.NamePlateBg.Opacity;
                    Speaker.Foreground = window.CharaName.Foreground;
                    Speaker.FontFamily = window.CharaName.FontFamily;
                    Speaker.FontSize = window.CharaName.FontSize;
                    Speaker.Content = CurrentScript.speaker;
                }
                Canvas.SetLeft(MessageWindow, Canvas.GetLeft(window.WindowContents));
                Canvas.SetTop(MessageWindow, Canvas.GetTop(window.WindowContents));
            }
            if (charas)
            {
                CharacterPictures.Children.Clear();
                if (0 < CurrentScript.charas.Count)
                {
                    var per = pmanager.Project.Size[0] / (CurrentScript.charas.Count * 2);
                    BitmapImage b;
                    Image img;
                    for (int i = 0; i < CurrentScript.charas.Count; i++)
                    {
                        b = Utils.CreateBmp(pmanager.GetResource("Characters", CurrentScript.charas[i]));
                        img = new Image()
                        {
                            Source = b,
                            Stretch = Stretch.Uniform,
                            Height = pmanager.Project.Size[1],
                        };
                        SetCharacter(img, per * (2 * (i + 1) - 1) - b.Width * Math.Min(Preview.Width / b.Width, Preview.Height / b.Height) / 2);
                    }
                }
                else
                {
                    NamePlate.Visibility = Visibility.Hidden;
                }
            }
            if (bg)
            {
                if (string.IsNullOrEmpty(CurrentScene.bg))
                {
                    Bg.Source = null;
                }
                else
                {
                    Bg.Source = Utils.CreateBmp(pmanager.GetResource(CurrentScene.bg));
                }
            }
        }

        private void SetCharacter(Image chara, double x)
        {
            chara.Opacity = 0;
            Canvas.SetLeft(chara, x);
            CharacterPictures.Children.Add(chara);
            var b = new DoubleAnimation()
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            chara.BeginAnimation(UIElement.OpacityProperty, b);
        }

        public void ChangeBackground(string path)
        {
            BgLabel.Content = Utils.CutString(Path.GetFileName(path), 8, lines: 2);
            Bg.Source = Utils.CreateBmp(pmanager.GetResource(CurrentScene.bg));
            ReLoad(script: false);
        }
        public void ChangeBackgroundBlack(bool reload=true, bool story=true)
        {
            // 黒画面
            Bg.Source = black_image;
            BgLabel.Content = "";
            Bg.Source = null;
            if (reload) ReLoad(script: false, story: story);
        }
        public void ChangeScene(SceneInfo info, bool changeBg=true)
        {
            if (CurrentScene == info) return;
            CurrentScene = info;
            if (changeBg)
            {
                if (0 < CurrentScene.bg.Length)
                {
                    ChangeBackground(pmanager.GetResource(CurrentScene.bg));
                }
                else
                {
                    ChangeBackgroundBlack();
                }
            }
            BgmLabel.Content = Utils.CutString(Path.GetFileName(CurrentScene.bgm), 8, lines: 2); ;
            ReLoad(story: false);
            LoadPreview();
        }
        public void ChangeStoryContent(ContentInfo info, bool refresh = false)
        {
            if (!refresh && CurrentContent == info) return;
            CurrentContent = info;
            if (CurrentContent.IsScenes())
            {
                CurrentEpisode = emanager.GetEpisode(CurrentContent.FullPath);
                var t = CurrentEpisode.GetThumbnail();
                if (t is null)
                {
                    ChangeBackgroundBlack(story: false);
                }
                else
                {
                    Bg.Source = t;
                    BgLabel.Content = Utils.CutString(Path.GetFileName(CurrentEpisode.smanager.scenes.First().Value.bg), 8, lines: 2);
                }
                ChangeScene(CurrentEpisode.smanager.scenes.First().Value, false);
            }
        }
        private void CloseSubWindows()
        {
            foreach (var w in OwnedWindows)
            {
                if (w is not null) (w as Window).Close();
            }
            GC.Collect();
        }

        private void OnExpanderCollapsed(object sender, RoutedEventArgs e)
        {
            ToolsExpander.Width = 22;
            PreviewBack.Width = ActualWidth - ToolsColumn.ActualWidth;
            AdjustPreviewSize();
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            ToolsExpander.Width = 205;
            PreviewBack.Width = Width - ToolsColumn.ActualWidth;
            AdjustPreviewSize();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var r = Math.Min(ActualWidth / DEFAULT_WIDTH, ActualHeight / DEFAULT_HEIGHT);
            if (r is not double.PositiveInfinity)
            {
                Scale.ScaleX = r;
                Scale.ScaleY = r;
            }
            AdjustPreviewSize();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectBg();
        }

        public void SelectBg()
        {
            SelectBgButton.IsEnabled = false;
            var bg = ResourceWindow.SelectImage(this);
            if (!string.IsNullOrEmpty(bg))
            {
                CurrentScene.bg = bg;
                if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
                {
                    foreach (var s in CurrentEpisode.smanager.scenes.Where(a => CurrentScene.order < a.Key))
                    {
                        s.Value.bg = bg;
                    }
                }
                ChangeBackground(bg);
            }
            SelectBgButton.IsEnabled = true;
        }

        private void OpenSelectedProject(object sender = null, EventArgs e = null)
        {
            if (IsProjectOpened)
            {
                var res = MessageBox.Show("プロジェクトを変更する前に現在のプロジェクトを保存しますか？", MainWindow.CAPTION, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (res)
                {
                    case MessageBoxResult.Yes:
                        Save(false);
                        break;
                    case MessageBoxResult.No:
                        break;
                    default:
                        return;
                }
            }
            new SelectProjectWindow(this).ShowDialog();
        }

        private void OpenCharacterManager(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_CharacterWindow))
            {
                _CharacterWindow = new(this);
                _CharacterWindow.Show();
            }
            else if (_CharacterWindow.IsLoaded)
            {
                _CharacterWindow.Activate();
                _CharacterWindow.Show();
            }
            else _CharacterWindow.Show();
        }

        private void OpenScriptEditor(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_ScriptWindow))
            {
                CurrentScriptIndex = 0;
                _ScriptWindow = new(this);
                _ScriptWindow.LoadLog();
                _ScriptWindow.Show();
            }
            else if (_ScriptWindow.IsLoaded)
            {
                _ScriptWindow.Activate();
                _ScriptWindow.LoadLog();
                _ScriptWindow.Show();
            }
            else
            {
                _ScriptWindow.LoadLog();
                _ScriptWindow.Show();
            }
        }
        private void OpenSceneEditor(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_SceneWindow))
            {
                _SceneWindow = new(this);
                _SceneWindow.Show();
            }
            else if (_SceneWindow.IsLoaded)
            {
                _SceneWindow.Activate();
                _SceneWindow.Show();
            }
            else _SceneWindow.Show();
        }
        private void OpenStoryEditor(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_StoryEditor))
            {
                _StoryEditor = new(this);
                _StoryEditor.Show();
            }
            else if (_StoryEditor.IsLoaded)
            {
                _StoryEditor.Activate();
                _StoryEditor.Show();
            }
            else _StoryEditor.Show();
        }

        private void OpenResourceManager(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(ResourceWindow))
            {
                ResourceWindow = new(this);
                ResourceWindow.Load();
                ResourceWindow.Show();
            }else if (ResourceWindow.IsLoaded)
            {
                ResourceWindow.Activate();
                ResourceWindow.Show();
            }
            else ResourceWindow.Show();
        }

        private void OnGameScreenSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsProjectOpened)
            {
                LoadPreview();
            }
        }
        private void OpenMessageDesigner(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_MessageWindowDesigner))
            {
                _MessageWindowDesigner = new(this);
                _MessageWindowDesigner.Show();
            }
            else if (_MessageWindowDesigner.IsLoaded)
            {
                _MessageWindowDesigner.Activate();
                _MessageWindowDesigner.Show();
            }
            else
            {
                _MessageWindowDesigner = new(this);
            }
        }

        private void OpenTitleScreenDesigner(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_TitleScreenDesigner))
            {
                _TitleScreenDesigner = new(this);
                _TitleScreenDesigner.Show();
            }
            else if (_TitleScreenDesigner.IsLoaded)
            {
                _TitleScreenDesigner.Activate();
                _TitleScreenDesigner.Show();
            }
            else
            {
                _TitleScreenDesigner = new(this);
            }
        }

        private void OpenSettingWindow(object sender = null, RoutedEventArgs e = null)
        {
            if (!IsAvailable(_SettingWindow))
            {
                _SettingWindow = new(this);
                _SettingWindow.Show();
            }
            else if (_SettingWindow.IsLoaded)
            {
                _SettingWindow.Activate();
                _SettingWindow.Show();
            }
            else
            {
                _SettingWindow = new(this);
            }
        }
        private static bool IsAvailable(Window? w)
        {
            return w is not null && w.IsLoaded && w.IsVisible;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            var pname = pmanager.ProjectName;
            var r = references;
            Task.Run(() =>
            {
                bmanager.Backup();
                var compiler = new GameBuilder(pname, pmanager.ProjectFile, r, mmanager, story, emanager);
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var res = compiler.Run(CurrentScene);
                        if (res.ReturnValue is not null)
                        {
                            ErrorNotifyWindow.Show($"{res.ReturnValue}:\n{res.Exception.Message}");
                        }
                    }catch (Exception e)
                    {
                        ErrorNotifyWindow.Show(e.Message);
                    }
                    RunButton.IsEnabled = true;
                });
            });
        }
        private void Save(bool dialog = true)
        {
            var d = "";
            if (IsCreated)
            {
                var dlg = new OpenFolderDialog()
                {
                    Multiselect = false
                };
                if (dlg.ShowDialog() is true)
                {
                    d = pmanager.ActualProjectDir;
                    var dst = Path.Combine(dlg.FolderName, pmanager.Project.Title);
                    Directory.CreateDirectory(dst);
                    pmanager.SetProjectDir(dst);
                }
                else
                {
                    return;
                }
            }
            emanager.Dump();
            pmanager.ApplyStory(story.StoryInfos);
            pmanager.SaveProject();
            if (IsCreated && d != "")
            {
                Directory.Delete(d, true);
                IsCreated = false;
            }
            if (dialog) MessageBox.Show("保存しました", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSaveButtonClicked(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void OnExportProjectButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                DefaultDirectory = pmanager.ProjectName,
            };
            if (dialog.ShowDialog() is true)
            {
                var progress = new BuildProgressWindow();
                progress.MainProgress.Maximum = 2;
                var data = new FilePackingData();
                updateProgressTimer = new()
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                updateProgressTimer.Tick += (sender, e) =>
                {
                    if (data.Length > 0)
                    {
                        var p = (int)(data.FinishedCount / data.Length * 100);
                        progress.VerbosePercentProgress.Content = $"{p}%";
                        progress.VerboseProgressLabel.Content = $"{Utils.CutString(data.FileName, 5)} {data.FinishedCount} / {data.Length}";
                        progress.VerboseProgress.Value = p;
                        progress.Refresh();
                    }
                };
                var c = new GameBuilder(pmanager.ProjectName, pmanager.ProjectFile, references, mmanager, story, emanager);
                updateProgressTimer.Start();
                Task.Run(() =>
                {
                    try
                    {
                        c.ExportProject(dialog.FolderName, progress, data, Dispatcher.Invoke);
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            updateProgressTimer.Stop();
                        });
                    }
                });
                progress.ShowDialog();
            }
        }

        private void OnExportScriptButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "Data files(*.json) | *.json | All files(*.*) | *.* ",
            };
            if (dialog.ShowDialog() is true)
            {
                var c = new GameBuilder(pmanager.ProjectName, pmanager.ProjectFile, references, mmanager, story, emanager);
                c.ExportScript(dialog.FileName);
                MessageBox.Show("出力が完了しました", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCreateExeButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                DefaultDirectory = pmanager.ProjectName,
            };
            if (dialog.ShowDialog() is true)
            {
                Save(false);
                bmanager.BackupForBuild();
                var progress = new BuildProgressWindow();
                progress.MainProgress.Maximum = 3;
                var data = new FilePackingData();
                updateProgressTimer = new()
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                updateProgressTimer.Tick += (sender, e) =>
                {
                    if (0 < data.Length)
                    {
                        var p = (int)((double)data.FinishedCount / data.Length * 100);
                        progress.VerbosePercentProgress.Content = $"{p}%";
                        progress.VerboseProgressLabel.Content = Utils.CutString(data.FileName, 15);
                        progress.VerboseProgress.Value = p;
                        progress.Refresh();
                    }
                };
                var c = new GameBuilder(pmanager.ProjectName, pmanager.ProjectFile, references, mmanager, story, emanager);
                updateProgressTimer.Start();
                Task.Run(() =>
                {
                    try
                    {
                        c.CreateExe(dialog.FolderName, progress, data, Dispatcher.Invoke);
                    }
                    catch(Exception e)
                    {
                        ErrorNotifyWindow.Show(this, e.Message);
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            updateProgressTimer.Stop();
                        });
                    }
                });
                progress.ShowDialog();
            }
        }

        private void SelectBgmButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBgm();
        }

        public void SelectBgm()
        {
            SelectBgmButton.IsEnabled = false;
            var bgm = ResourceWindow.SelectAudio(this);
            if (bgm is not null)
            {
                CurrentScene.bgm = bgm;
                if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
                {
                    foreach (var s in CurrentEpisode.smanager.scenes)
                    {
                        if (CurrentScene.order < s.Value.order)
                        {
                            s.Value.bgm = bgm;
                        }
                    }
                }
                BgmLabel.Content = Utils.CutString(Path.GetFileName(bgm), 8, lines: 2);
            }
            SelectBgmButton.IsEnabled = true;
        }

        private void RemoveBgButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentScene.bg = "";
            if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
            {
                foreach (var s in CurrentEpisode.smanager.scenes)
                {
                    if (CurrentScene.order < s.Value.order)
                    {
                        s.Value.bg = "";
                    }
                }
            }
            ChangeBackgroundBlack();
        }

        private void RemoveBgmButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentScene.bgm = "";
            if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
            {
                foreach (var s in CurrentEpisode.smanager.scenes)
                {
                    if (CurrentScene.order < s.Value.order)
                    {
                        s.Value.bgm = "";
                    }
                }
            }
            BgmLabel.Content = "";
        }
    }
}