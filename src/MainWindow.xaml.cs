using EmeralEngine.Resource.Character;
using EmeralEngine.Project;
using EmeralEngine.Scene;
using EmeralEngine.Builder;
using EmeralEngine.Resource;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using System.Windows.Threading;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using EmeralEngine.Setting;
using EmeralEngine.Script;
using EmeralEngine.Story;
using EmeralEngine.MessageWindow;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using EmeralEngine.TitleScreen;
using Microsoft.VisualBasic.FileIO;
using EmeralEngine.Core;
using EmeralEngine.Notify;

namespace EmeralEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string CAPTION = "EmeralEngine v0.1";
        private const double DEFAULT_WIDTH = 800;
        private const double DEFAULT_HEIGHT = 450;
        private double PREVIEW_DEFAULT_WIDTH = 600;
        private double PREVIEW_MAX_DEFAULT_WIDTH = 750;
        private int NowScriptIndex;
        public static ProjectManager pmanager = new();
        public MessageWindowManager mmanager;
        public StoryManager story;
        public EpisodeManager emanager;
        private BackupManager bmanager;
        private Managers Managers;
        private CharacterWindow _CharacterWindow;
        private ScriptEditor _ScriptWindow;
        private SceneWindow _SceneWindow;
        private StoryEditor _StoryEditor;
        public ResourceWindow ResourceWindow;
        private MessageWindowDesigner _MessageWindowDesigner;
        private TitleScreenDesigner _TitleScreenDesigner;
        private SettingWindow _SettingWindow;
        public SceneInfo now_scene;
        public ContentInfo now_content;
        public EpisodeInfo now_episode;
        private Assembly[] references;
        private DispatcherTimer backup_timer;
        private DispatcherTimer updateProgressTimer;
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
        public MainWindow()
        {
            references = AppDomain.CurrentDomain.GetAssemblies();
            InitializeComponent();
            ContentRendered += (sender, e) =>
            {
                var r = Math.Min(ActualWidth / DEFAULT_WIDTH, ActualHeight / DEFAULT_HEIGHT);
                Scale.ScaleX = r;
                Scale.ScaleY = r;
                OpenSelectProject();
            };
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
            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Dispatcher.Invoke(() =>
                {
                    ErrorNotifyWindow.Show(e.Exception.Message);
                    AskSave();
                });
            };
            Closing += (sender, e) =>
            {
                e.Cancel = AskSave();
            };
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
            if (pmanager.Temp is not null)
            {
                pmanager.Temp.Dispose();
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
        public void LoadProject(string name)
        {
            pmanager.LoadProject(name);
            Title = $"{CAPTION} {pmanager.ProjectName} ロード中...";
            Refresh();
            backup_timer.Stop();
            CloseSubWindows();
            NowScriptIndex = -1;
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
            if (emanager.episodes.Count == 0)
            {
                now_episode = emanager.New();
            }
            else
            {
                now_episode = emanager.episodes.Values.First();
            }
            if (story.stories.Count == 0)
            {
                now_content = story.New(now_episode.path);
                now_content.path = now_episode.path;
                pmanager.Project.Story.Add(now_content);
                pmanager.SaveProject();
            }
            else
            {
                now_content = story.stories.First().Value;
            }
            now_scene = now_episode.smanager.scenes.First().Value;
            if (0 < now_scene.bg.Length) ChangeBackground(pmanager.GetResource(now_scene.bg));
            else ChangeBackgroundBlack();
            BgLabel.Content = Utils.CutString(now_scene.bg, 8, lines: 2);
            BgmLabel.Content = now_scene.bgm;
            if (pmanager.Project.Startup.Story) OpenStoryEditor();
            if (pmanager.Project.Startup.Scene) OpenSceneEditor();
            if (pmanager.Project.Startup.Script) OpenScriptEditor();
            else
            {
                _ScriptWindow = new(this);
            }
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
                if (ResourceExpander.IsExpanded)
                {
                    r = Math.Min(PREVIEW_DEFAULT_WIDTH / w, ResourceExpander.ActualHeight / h);
                }
                else
                {
                    r = Math.Min(PREVIEW_MAX_DEFAULT_WIDTH / w, ResourceExpander.ActualHeight / h);
                }
                Preview.Width = w * r;
                Preview.Height = h * r;
                PreviewScale.ScaleX = r;
                PreviewScale.ScaleY = r;
                NowScriptIndex = _ScriptWindow.current_index;
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
            if (script && scriptIsAvailable)
            {
                NowScriptIndex = _ScriptWindow.current_index;
                Script.Text = _ScriptWindow.now_script.script;
            }
            if (msw)
            {
                var window = mmanager.windows[now_scene.msw];
                MessageWindow.Width = window.Width;
                MessageWindow.Height = window.Height;
                MessageWindowBg.Width = window.Width;
                MessageWindowBg.Height = window.Height;
                MessageWindow.Background = Utils.GetBrush(window.BgColor);
                MessageWindow.Background.Opacity = window.BgColorAlpha;
                MessageWindowBg.Opacity = window.BgAlpha;
                if (string.IsNullOrEmpty(window.Bg))
                {
                    MessageWindowBg.Source = null;
                }
                else
                {
                    MessageWindowBg.Source = Utils.CreateBmp(MainWindow.pmanager.GetResource(window.Bg));
                }
                Canvas.SetLeft(Script, window.ScriptLeftPos);
                Canvas.SetTop(Script, window.ScriptTopPos);
                Script.Width = window.ScriptWidth;
                Script.FontSize = window.FontSize;
                Script.FontFamily = new FontFamily(window.Font);
                Script.Foreground = Utils.GetBrush(window.TextColor);
                if (_ScriptWindow.now_script is null)
                {

                }
                else if (string.IsNullOrWhiteSpace(_ScriptWindow.now_script.speaker))
                {
                    if (scriptIsAvailable) NamePlate.Visibility = Visibility.Hidden;
                }
                else
                {
                    if (scriptIsAvailable) NamePlate.Visibility = Visibility.Visible;
                    Canvas.SetLeft(NamePlate, window.NamePlateLeftPos);
                    Canvas.SetBottom(NamePlate, window.NamePlateBottomPos);
                    MessageWindow.Width = window.Width;
                    MessageWindow.Height = window.Height;
                    NamePlate.Width = window.NamePlateWidth;
                    NamePlate.Height = window.NamePlateHeight;
                    NamePlate.Background = Utils.GetBrush(window.NamePlateBgColor);
                    NamePlate.Background.Opacity = window.NamePlateBgColorAlpha;
                    if (!string.IsNullOrEmpty(window.NamePlateBgImage))
                    {
                        NamePlateBgImage.Source = Utils.CreateBmp(pmanager.GetResource(window.NamePlateBgImage));
                    }
                    NamePlateBgImage.Opacity = window.NamePlateBgImageAlpha;
                    Speaker.Foreground = Utils.GetBrush(window.NameFontColor);
                    Speaker.FontFamily = new FontFamily(window.NameFont);
                    Speaker.FontSize = window.NameFontSize;
                    Speaker.Content = _ScriptWindow.now_script.speaker;
                }
                Canvas.SetLeft(MessageWindow, window.WindowLeftPos);
                Canvas.SetBottom(MessageWindow, window.WindowBottom);
            }
            if (charas && scriptIsAvailable)
            {
                CharacterPictures.Children.Clear();
                var per = pmanager.Project.Size[0] / (_ScriptWindow.now_script.charas.Count + 1);
                BitmapImage b;
                for (int i = 0; i < _ScriptWindow.now_script.charas.Count; i++)
                {
                    b = Utils.CreateBmp(pmanager.GetResource("Characters", _ScriptWindow.now_script.charas[i]));
                    SetCharacter(new Image()
                    {
                        Source = b,
                        Stretch = Stretch.Uniform,
                        Height = pmanager.Project.Size[1]
                    }, per * (i + 1) - b.Width / 2);
                }
            }
            if (bg)
            {
                if (string.IsNullOrEmpty(now_scene.bg))
                {
                    Bg.Source = null;
                }
                else
                {
                    Bg.Source = Utils.CreateBmp(pmanager.GetResource(now_scene.bg));
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
            Bg.Source = Utils.CreateBmp(pmanager.GetResource(now_scene.bg));
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
            if (now_scene == info) return;
            now_scene = info;
            if (changeBg)
            {
                if (0 < now_scene.bg.Length)
                {
                    ChangeBackground(pmanager.GetResource(now_scene.bg));
                }
                else
                {
                    ChangeBackgroundBlack();
                }
            }
            BgmLabel.Content = Utils.CutString(Path.GetFileName(now_scene.bgm), 8, lines: 2); ;
            ReLoad(story: false);
            LoadPreview();
        }
        public void ChangeStoryContent(ContentInfo info, bool refresh = false)
        {
            if (!refresh && now_content == info) return;
            now_content = info;
            if (now_content.IsScenes())
            {
                now_episode = emanager.GetEpisode(now_content.path);
                var t = now_episode.GetThumbnail();
                if (t is null)
                {
                    ChangeBackgroundBlack(story: false);
                }
                else
                {
                    Bg.Source = t;
                    BgLabel.Content = Utils.CutString(Path.GetFileName(now_episode.smanager.scenes.First().Value.bg), 8, lines: 2);
                }
                ChangeScene(now_episode.smanager.scenes.First().Value, false);
            }
        }
        private void CloseSubWindows()
        {
            foreach (var w in OwnedWindows)
            {
                if (w is not null) (w as Window).Close();
            }
        }

        private void OnExpanderCollapsed(object sender, RoutedEventArgs e)
        {
            AdjustPreviewSize();
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
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
                now_scene.bg = bg;
                if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
                {
                    foreach (var s in now_episode.smanager.scenes.Where(a => now_scene.order < a.Key))
                    {
                        s.Value.bg = bg;
                    }
                }
                ChangeBackground(bg);
            }
            SelectBgButton.IsEnabled = true;
        }

        private void OpenSelectProject(object sender = null, EventArgs e = null)
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
                NowScriptIndex = 0;
                _ScriptWindow = new(this, NowScriptIndex);
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
                var compiler = new GameBuilder(pname, r, mmanager, story, emanager);
                Dispatcher.BeginInvoke(() =>
                {
                    var res = compiler.Run(now_scene);
                    if (res.ReturnValue is not null)
                    {
                        ErrorNotifyWindow.Show($"{res.ReturnValue}:\n{res.Exception.Message}");
                    }
                    RunButton.IsEnabled = true;
                });
            });
        }
        private void Save(bool dialog = true)
        {
            emanager.Dump();
            if (Directory.Exists(pmanager.ActualProjectEpisodesDir))
            {
                Directory.Delete(pmanager.ActualProjectEpisodesDir, true);
            }
            Directory.CreateDirectory(pmanager.ActualProjectEpisodesDir);
            FileSystem.CopyDirectory(pmanager.ProjectEpisodesDir, pmanager.ActualProjectEpisodesDir, true);
            if (File.Exists(pmanager.ProjectTitleScreen))
            {
                File.Copy(pmanager.ProjectTitleScreen, pmanager.ActualProjectTitleScreen, true);
            }
            pmanager.ApplyStory(story.StoryInfos);
            pmanager.SaveProject();
            mmanager.Dump(Path.Combine(pmanager.ActualProjectDir, MessageWindowManager.FILENAME));
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
                var c = new GameBuilder(pmanager.ProjectName, references, mmanager, story, emanager);
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
                        var p = (int)(data.FinishedCount / data.Length * 100);
                        progress.VerbosePercentProgress.Content = $"{p}%";
                        progress.VerboseProgressLabel.Content = Utils.CutString(data.FileName, 15);
                        progress.VerboseProgress.Value = p;
                        progress.Refresh();
                    }
                };
                var c = new GameBuilder(pmanager.ProjectName, references, mmanager, story, emanager);
                updateProgressTimer.Start();
                Task.Run(() =>
                {
                    try
                    {
                        c.CreateExe(dialog.FolderName, progress, data, Dispatcher.Invoke);
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
                now_scene.bgm = bgm;
                if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
                {
                    foreach (var s in now_episode.smanager.scenes)
                    {
                        if (now_scene.order < s.Value.order)
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
            now_scene.bg = "";
            if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
            {
                foreach (var s in now_episode.smanager.scenes)
                {
                    if (now_scene.order < s.Value.order)
                    {
                        s.Value.bg = "";
                    }
                }
            }
            ChangeBackgroundBlack();
        }

        private void RemoveBgmButton_Click(object sender, RoutedEventArgs e)
        {
            now_scene.bgm = "";
            if (pmanager.Project.SceneSettings.ChangeLatterBgWhenChanged)
            {
                foreach (var s in now_episode.smanager.scenes)
                {
                    if (now_scene.order < s.Value.order)
                    {
                        s.Value.bgm = "";
                    }
                }
            }
            BgmLabel.Content = "";
        }
    }
}