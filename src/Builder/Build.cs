using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using EmeralEngine.Notify;
using EmeralEngine.Project;
using EmeralEngine.Resource.CustomTransition;
using EmeralEngine.Scene;
using EmeralEngine.Script;
using EmeralEngine.Setting;
using EmeralEngine.Story;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace EmeralEngine.Builder
{
    class GameBuilder
    {
        public const string DOTNET_DIR = "dotnet";
        public static Regex SourceRegex = new Regex(@" Source\s*=\s*""([^""]+)""");
        private string title;
        private MessageWindowManager mmanager;
        private StoryManager story;
        private EpisodeManager emanager;
        private Assembly[] references;
        private Dictionary<string, string> hashTable;
        public GameBuilder(string title, Assembly[] refs, MessageWindowManager m, StoryManager s, EpisodeManager e)
        {
            this.title = title;
            mmanager = m;
            story = s;
            emanager = e;
            references = refs;
        }

        public void ExportProject(string dest, BuildProgressWindow progress, FilePackingData data, Action<Action> dispatcher)
        {
            var baseDir = Path.Combine(dest, title);
            FileSystem.CopyDirectory(_ExportProject(dest, data, progress, dispatcher).baseDir, baseDir, UIOption.AllDialogs, UICancelOption.DoNothing);
            dispatcher(() =>
            {
                progress.MainProgress.Value = progress.MainProgress.Maximum;
                progress.Close();
            });
            MessageBox.Show("出力が完了しました", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private CompilationFiles? _ExportProject(string dest, FilePackingData data, BuildProgressWindow progress, Action<Action> dispatcher)
        {
            var baseDir = Path.Combine(dest, title);
            var d = Path.Combine(baseDir, "datas");
            var target = Path.Combine(MainWindow.pmanager.ActualProjectDir, DOTNET_DIR);
            if (File.Exists(baseDir))
            {
                MessageBox.Show($"{baseDir}と同名のファイルが存在しています", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            else if (Directory.Exists(target))
            {
                try
                {
                    Directory.Delete(Path.Combine(target, "bin"), true);
                    Directory.Delete(Path.Combine(target, "obj"), true);
                }
                catch { }
            }
            else
            {
                ProjectManager.InitDotnet();
            }
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(d);
            dispatcher(() =>
            {
                progress.Label.Content = $"リソースをパッキング中... {progress.MainProgress.Value} / {progress.MainProgress.Maximum}";
            });
            var resources = MainWindow.pmanager.GetAllResources();
            var packer = new ResourcePacker(resources);
            hashTable = packer.Pack(d, data);
            dispatcher(() =>
            {
                progress.MainProgress.Value += 1;
                progress.Label.Content = $"コードを生成中... {progress.MainProgress.Value} / {progress.MainProgress.Maximum}";
            });
            var files = new CompilationFiles(target);
            File.WriteAllText(files.savedatamanager, """
                using System;
                using System.Collections.Generic;
                using System.IO;
                using System.Linq;
                using System.Security.Cryptography;
                using System.Text;
                using System.Text.Json;
                using System.Threading.Tasks;
                using System.Windows;

                namespace Game.Core
                {
                    internal class SaveDataManager
                    {
                        private const int HASH_LENGTH = 32;
                        private string FILE = Path.Combine("datas", "data0.dat");
                        public Dictionary<int, SaveData> Datas;

                        public SaveDataManager()
                        {
                            if (File.Exists(FILE))
                            {
                                Load();
                            }
                            else
                            {
                                Datas = new();
                            }
                        }

                        public bool Contains(int id)
                        {
                            return Datas.ContainsKey(id);
                        }

                        private string HashAsString(byte[] b)
                        {
                            return BitConverter.ToString(b);
                        }

                        private string HashAsString(List<byte> b)
                        {
                            return HashAsString(b.ToArray());
                        }

                        private string GetString(List<byte> b)
                        {
                            return Encoding.UTF8.GetString(b.ToArray());
                        }

                        public string Register(int id, int scene, string script, int scriptid, byte[] shot)
                        {
                            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            Datas[id] = new SaveData()
                            {
                                SceneId = scene,
                                ScriptId = scriptid,
                                Script = script,
                                Time = date,
                                ScreenShot = shot
                            };
                            Dump();
                            return date;
                        }

                        public void Remove(int id)
                        {
                            Datas.Remove(id);
                        }

                        public void Dump()
                        {
                            var data = JsonSerializer.Serialize(Datas);
                            using (var sha256 = SHA256.Create())
                            using (var f = new FileStream(FILE, FileMode.Create, FileAccess.Write))
                            {
                                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                                foreach (var b in hash)
                                {
                                    f.WriteByte((byte)~b);
                                }
                                foreach (var c in data)
                                {
                                    f.WriteByte((byte)~c);
                                }
                            }
                        }

                        public void Load()
                        {
                            var hash = new List<byte>();
                            var data = new List<byte>();
                            try
                            {
                                using (var f = new FileStream(FILE, FileMode.Open, FileAccess.Read))
                                {
                                    for (int _=0; _<HASH_LENGTH; _++)
                                    {
                                        hash.Add((byte)~f.ReadByte());
                                    }
                                    var b = f.ReadByte();
                                    while (b != -1)
                                    {
                                        data.Add((byte)~b);
                                        b = f.ReadByte();
                                    }
                                }
                                byte[] h;
                                using (var sha256 = SHA256.Create())
                                {
                                    h = sha256.ComputeHash(data.ToArray());
                                }
                                if (HashAsString(hash) == HashAsString(h))
                                {
                                    Datas = JsonSerializer.Deserialize<Dictionary<int, SaveData>>(GetString(data));
                                }
                                else
                                {
                                    File.Delete(FILE);
                                    MessageBox.Show("セーブデータの破損が確認されました\nセーブデータを初期化し、ゲームを終了します");
                                    Environment.Exit(1);
                                }
                            }
                            catch
                            {
                                File.Delete(FILE);
                                MessageBox.Show("セーブデータの破損が確認されました\nセーブデータを初期化し、ゲームを終了します");
                                Environment.Exit(1);
                            }
                        }
                    }

                    class SaveData
                    {
                        public string Script { get; set; }
                        public int ScriptId {  get; set; }
                        public int SceneId {  get; set; }
                        public byte[] ScreenShot {  get; set; }
                        public string Time {  get; set; }
                    }
                }
                
                """);
            File.WriteAllText(files.main, GenerateCompilationCode());
            File.WriteAllText(files.main_xaml, $$"""
                <Window x:Class="Game.MainWindow"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                        xmlns:local="clr-namespace:Game"
                        mc:Ignorable="d"
                        Title="{{MainWindow.pmanager.Project.Title}}" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}" Background="Black" ResizeMode="CanMinimize" WindowStartupLocation="CenterScreen">
                        <DockPanel>
                            <DockPanel.LayoutTransform>
                                <ScaleTransform x:Name="Scale"/>
                            </DockPanel.LayoutTransform>
                            <Menu Name="WindowMenu" DockPanel.Dock="Top" IsMainMenu="True" Visibility="Collapsed">
                                <MenuItem Header="ファイル">
                                    <MenuItem Name="SaveMenu" Header="セーブ" Click="OpenSaveDialog"/>
                                    <MenuItem Name="LoadMenu" Header="ロード" Click="OpenLoadDialog"/>
                                </MenuItem>
                                <MenuItem Header="ウィンドウ">
                                    <MenuItem Name="MaximizeMenu" Header="最大化" IsCheckable="True" IsChecked="False"/>
                                </MenuItem>
                            </Menu>
                            <Frame Name="Screen" NavigationUIVisibility="Hidden"/>
                        </DockPanel>
                </Window>
                """);
            var table = new Dictionary<string, string>();
            var xaml = HandleXaml(MainWindow.pmanager.ReadTitleScreenXaml(MainWindow.pmanager.ActualProjectResourceDir), table);
            var sources = new StringBuilder();
            var defines = new StringBuilder();
            foreach (var p in table)
            {
                sources.AppendLine($"""
                    {p.Value} = MainWindow.CreateBmp(MainWindow.GetResource(@"{hashTable[p.Key]}"));
                    """);
                defines.AppendLine($$"""
                    private BitmapImage _{{p.Value}};
                    public BitmapImage {{p.Value}} {
                        get => _{{p.Value}};
                        set{
                            _{{p.Value}} = value;
                            OnPropertyChanged(nameof({{p.Value}}));
                        }
                    }
                    """);
            }
            File.WriteAllText(files.title_xaml, $"""
                <Page x:Class="Game.TitlePage"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    {xaml}
                </Page>
                """);
            File.WriteAllText(files.title, $$"""
                using System.Windows;
                using System.Windows.Input;
                using System.Windows.Controls;
                using System.Windows.Media.Imaging;
                using System.Windows.Media;
                using System.Windows.Media.Animation;
                using System.Threading.Tasks;
                using System.Windows.Threading;
                using System.ComponentModel;
                using System.IO;
                using System;
                
                namespace Game
                {
                    public partial class TitlePage : Page
                    {
                        public TitlePage(MainWindow w)
                        {
                            InitializeComponent();
                            var data = new Images();
                            DataContext = data;
                            Loaded += (sender, e) => {
                                w.SaveMenu.IsEnabled = false;
                                data.Load();
                            };
                            Unloaded += (sender, e) => {
                                w.SaveMenu.IsEnabled = true;
                            };
                        }
                    }

                    public class Images : INotifyPropertyChanged
                    {
                        {{defines}}

                        public event PropertyChangedEventHandler PropertyChanged;
                        protected void OnPropertyChanged(string name)
                        {
                            if (PropertyChanged is not null)
                            {
                                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
                            }
                        }
                    
                        public void Load()
                        {
                            {{sources}}
                        }
                    }
                }
                """);
            File.WriteAllText(files.gamepage_xaml, $"""
                <Page
                    x:Class="Game.GamePage"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    {GenerateGameUIXaml(false)}
                </Page>
                """);
            File.WriteAllText(files.savedata_xaml, """
                 <Page x:Class="Game.Core.SaveDataPage"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                      Background="Gray" MouseRightButtonUp="OnMouseRightButtonUp">

                    <UniformGrid Columns="3" Rows="3">
                        <StackPanel Name="Data1" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot1" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time1"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data2" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot2" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time2"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data3" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot3" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time3"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data4" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot4" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time4"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data5" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot5" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time5"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data6" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot6" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time6"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data7" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot7" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time7"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data8" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot8" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time8"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data9" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image IsHitTestVisible="False" Name="ScreenShot9" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time9"/>
                            </Grid>
                        </StackPanel>
                    </UniformGrid>
                </Page>
                
                """);
            File.WriteAllText(files.savedata, $$"""
                using System;
                using System.IO;
                using System.Reflection;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Windows;
                using System.Windows.Controls;
                using System.Windows.Controls.Ribbon;
                using System.Windows.Data;
                using System.Windows.Documents;
                using System.Windows.Input;
                using System.Windows.Media;
                using System.Windows.Media.Imaging;
                using System.Windows.Navigation;
                using System.Windows.Shapes;

                namespace Game.Core
                {
                    /// <summary>
                    /// SaveDataPage.xaml の相互作用ロジック
                    /// </summary>
                    public partial class SaveDataPage : Page
                    {
                        private const double RATE = 2 / 3; // w : h = 3 : 2
                        private const int SEPARATE = 50;
                        private const int SEPARATE4 = SEPARATE * 4;
                        private const double DPI = 96.0; 
                        private double DataWidth, DataHeight;
                        private SaveDataManager smanager;
                        private GamePage Game;
                        private Frame Screen;
                        public bool IsSaveMode;

                        public SaveDataPage(GamePage p, Frame s)
                        {
                            InitializeComponent();
                            DataContext = this;
                            DataWidth = Width / 3 - SEPARATE4;
                            DataHeight = DataWidth * RATE;
                            smanager = new();
                            Game = p;
                            Screen = s;
                            MouseRightButtonDown += (sender, e) => {
                                Screen.GoBack();
                            };
                            Loaded += (sender, e) =>
                            {
                                foreach (var d in smanager.Datas)
                                {
                                    (FindName($"ScreenShot{d.Key}") as Image).Source = MainWindow.CreateBmp(d.Value.ScreenShot);
                                    (FindName($"Time{d.Key}") as Label).Content = d.Value.Time;
                                }
                            };
                        }

                        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
                        {
                            Screen.GoBack();
                        }

                        private byte[] GetScreenShot()
                        {
                            var b = new RenderTargetBitmap((int)Game.RenderSize.Width, (int)Game.RenderSize.Height, DPI, DPI, PixelFormats.Pbgra32);
                            b.Render(Game);
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(b));
                            using (var m = new MemoryStream())
                            {
                                encoder.Save(m);
                                return m.ToArray();
                            }
                        }

                        private void  OnDataMouseEnter(object sender, MouseEventArgs e)
                        {
                            {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $"""
                                MainWindow.PlaySound(@"{hashTable[MainWindow.pmanager.Project.MouseOverSE]}");
                            """)}}
                        }

                        private void OnDataMouseLeftUp(object sender, MouseButtonEventArgs e)
                        {
                            {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"""
                                MainWindow.PlaySound(@"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}");
                            """)}}
                            var n = int.Parse((e.Source as FrameworkElement).Name[4..]);
                            if (IsSaveMode)
                            {
                                var shot = GetScreenShot();
                                (FindName($"ScreenShot{n}") as Image).Source = MainWindow.CreateBmp(shot);
                                (FindName($"Time{n}") as Label).Content = smanager.Register(n, Game.CurrentScene, Game.CurrentScript, Game.CurrentScriptId, shot);
                            }
                            else
                            {
                                if (smanager.Contains(n))
                                {
                                    var d = smanager.Datas[n];
                                    Game.GoTo(d.SceneId, d.ScriptId);
                                }
                            }
                        }
                    }
                }
                
                """);
            File.WriteAllText(files.gamepage, $$"""
                using NAudio.Wave;
                using System.Windows;
                using System.Windows.Input;
                using System.Windows.Controls;
                using System.Windows.Media.Imaging;
                using System.Windows.Media;
                using System.Windows.Media.Animation;
                using System.Threading.Tasks;
                using System.Windows.Threading;
                using System.IO;
                using System.Reflection;
                using System;

                namespace Game
                {
                    public partial class GamePage : Page
                    {
                        public int CurrentScriptId, CurrentScene;
                        public string CurrentScript;
                        private WaveOutEvent BgmPlayer;
                        private MainWindow window;
                        private bool IsHandling;
                        private bool IsNowScripting;
                        private MouseButtonEventHandler OnMouseLeftDown = (sender, e) => {};
                        private RoutedEventHandler MediaEnded;
                        private TempFile _movieFile;

                        public GamePage(MainWindow w)
                        {
                            InitializeComponent();
                            window = w;
                            _movieFile = new();
                            BgmPlayer = new WaveOutEvent() {
                                Volume = (float)0.5
                            };
                            MainPanel.MouseRightButtonDown += (sender, e) => {
                                if (!IsHandling) MessageWindowCanvas.Visibility = MessageWindowCanvas.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                            };
                            Unloaded += (sender, e) => {
                                if (_movieFile is not null)
                                {
                                    _movieFile.Dispose();
                                }
                            };
                        }
                
                        private void SetCharacter(Image chara, double x)
                        {
                            chara.Opacity = 0;
                            Canvas.SetLeft(chara, x);
                            CharacterPictures.Children.Add(chara);
                            var b = new DoubleAnimation() {
                                From = 0.0,
                                To = 1.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(300))
                            };
                            chara.BeginAnimation(UIElement.OpacityProperty, b);
                        }
                
                        private void RemoveCharas()
                        {
                            foreach (UIElement c in CharacterPictures.Children)
                            {
                                var b = new DoubleAnimation() {
                                    From = 1.0,
                                    To = 0.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                                };
                                b.Completed += (sender, e) => {
                                    CharacterPictures.Children.Remove(c);
                                };
                                c.BeginAnimation(UIElement.OpacityProperty, b);
                            }
                        }
                        
                
                        private void PlayBgm(byte[] b)
                        {
                            BgmPlayer.Stop();
                            BgmPlayer = new WaveOutEvent();
                            var s = new MemoryStream(b);
                            try
                            {
                                 BgmPlayer.Init(new LoopStream(new WaveFileReader(s)));   
                            }
                            catch
                            {
                                try
                                {
                                    BgmPlayer.Init(new LoopStream(new Mp3FileReader(s)));
                                }
                                catch
                                {
                                    BgmPlayer.Init(new LoopStream(new AiffFileReader(s)));
                                }
                            }
                            BgmPlayer.Play();
                        }
                
                        private void FinishBgm()
                        {
                            BgmPlayer.Stop();
                        }

                        public void Clear()
                        {
                            Bg.Source = null;
                            MoviePlayer.Source = null;
                            if (_movieFile is not null)
                            {
                                _movieFile.Dispose();
                            }
                            FinishBgm();
                            RemoveCharas();
                            Script.Text = "";
                            MessageWindowCanvas.Visibility = Visibility.Hidden;
                        }

                        public void GoTo(int scene, int script)
                        {
                            Clear();
                            window.Screen.Navigate(this);
                            var f = GetType().GetMethod($"Scene{scene}", BindingFlags.Instance | BindingFlags.Public);
                            f.Invoke(this, new object[] { script });
                        }

                       {{GenerateStoryCode()}}
                    }

                    public class TempFile: IDisposable
                    {
                        public string path;
                        private  bool _disposed = false;
                        public TempFile(string suffix="")
                        {
                            path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + suffix);
                        }

                        public void Write(byte[] contents)
                        {
                            File.WriteAllBytes(path, contents);
                        }

                        public void Remove()
                        {
                            File.Delete(path);
                        }

                        public void Dispose()
                        {
                            Dispose(true);
                        }

                        protected virtual void Dispose(bool disposing)
                        {
                            if (!_disposed)
                            {
                                if (disposing)
                                {
                                    Remove();
                                }
                                _disposed = true;
                            }
                        }
                    }

                    public class LoopStream : WaveStream
                    {
                        WaveStream sourceStream;
                    
                        public LoopStream(WaveStream sourceStream)
                        {
                            this.sourceStream = sourceStream;
                            this.EnableLooping = true;
                        }
                    
                        public bool EnableLooping { get; set; }
                    
                        public override WaveFormat WaveFormat
                        {
                            get { return sourceStream.WaveFormat; }
                        }
                    
                        public override long Length
                        {
                            get { return sourceStream.Length; }
                        }
                    
                        public override long Position
                        {
                            get { return sourceStream.Position; }
                            set { sourceStream.Position = value; }
                        }
                    
                        public override int Read(byte[] buffer, int offset, int count)
                        {
                            int totalBytesRead = 0;
                    
                            while (totalBytesRead < count)
                            {
                                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                                if (bytesRead == 0)
                                {
                                    if (sourceStream.Position == 0 || !EnableLooping)
                                    {
                                        // something wrong with the source stream
                                        break;
                                    }
                                    // loop
                                    sourceStream.Position = 0;
                                }
                                totalBytesRead += bytesRead;
                            }
                            return totalBytesRead;
                        }
                    }
                }
                """);
            return files;
        }
        private string HandleXaml(string xaml, Dictionary<string, string> d)
        {
            var n = 1;
            return SourceRegex.Replace(xaml, s =>
            {
                var p = Path.GetRelativePath(MainWindow.pmanager.ActualProjectResourceDir, s.Groups[1].Value);
                var property = $"Image{n}";
                d.Add(p, property);
                n++;
                return $" Source=\"{{Binding {property}}}\"";
            });
        }
        public void CreateExe(string dest, BuildProgressWindow progress, FilePackingData data, Action<Action> dispatcher)
        {
            var files = _ExportProject(dest, data, progress, dispatcher);
            if (files is null)
            {
                return;
            }
            dispatcher(() =>
            {
                progress.MainProgress.Value += 1;
                progress.Label.Content = $"ビルド中... {progress.MainProgress.Value} / {progress.MainProgress.Maximum}";
            });
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"publish \"{files.csproj}\" -c Release --self-contained true -r win-x64 -p:ReadyToRun=true -p:AssemblyName=\"{MainWindow.pmanager.ProjectName}\" -o \"{Path.Combine(dest, MainWindow.pmanager.ProjectName)}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };
            Task.Run(() =>
            {
                p.Start();
                var res = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                dispatcher(() =>
                {
                    progress.MainProgress.Value = progress.MainProgress.Maximum;
                    progress.Close();
                    if (p.ExitCode == 0)
                    {
                        MessageBox.Show("出力が完了しました", MainWindow.CAPTION, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorNotifyWindow.Show(res);
                    }
                });
            });
        }

        private string GenerateGameUIXaml(bool IsScript=true)
        {
            var for_script = """
                        <DockPanel.LayoutTransform>
                            <ScaleTransform x:Name="Scale"/>
                        </DockPanel.LayoutTransform>
                        <Menu Name="WindowMenu" DockPanel.Dock="Top" Visibility="Collapsed">
                            <MenuItem Header="ウィンドウ">
                                <MenuItem Name="MaximizeMenu" Header="最大化" IsCheckable="True" IsChecked="False"/>
                            </MenuItem>
                        </Menu>
                """;
            return $$"""
                    <DockPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Name="MainPanel" Background="Black">
                        {{(IsScript? for_script : "")}}
                        <Grid Name="MainGrid" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}">
                            <Image Name="Bg" Stretch="Uniform"/>
                            <Canvas Name="CharacterPictures"/>
                            <Canvas Name="MessageWindowCanvas">
                                <StackPanel Name="MessageWindow" VerticalAlignment="Bottom">
                                    <Canvas>
                                        <Image Name="MessageWindowBg" Stretch="Fill"/>
                                        <TextBlock Name="Script"/>
                                    </Canvas>
                                </StackPanel>
                                <Canvas Name="NamePlate">
                                    <Image x:Name="NamePlateBgImage" Height="{Binding ActualHeight, ElementName=NamePlate}" Width="{Binding ActualWidth, ElementName=NamePlate}" Stretch="Fill"/>
                                    <Label Name="Speaker" Content="名前" FontSize="30" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Canvas>
                            </Canvas>
                            <MediaElement Name="MoviePlayer" Stretch="Uniform" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}" LoadedBehavior="Manual"/>
                            <Rectangle Name="Transition" Opacity="0"/>
                        </Grid>
                    </DockPanel>
                """;
        }

        private string ConvertPath(string path)
        {
            if (hashTable is null)
            {
                return path;
            }
            else
            {
                return hashTable[path];
            }
        }

        public ScriptState<object> Run(SceneInfo start)
        {
            File.WriteAllText(Path.Combine(MainWindow.pmanager.ActualProjectDir, "script.cs"), GenerateScriptCode(start));
            var script = CSharpScript.Create(GenerateScriptCode(start), ScriptOptions.Default
                                                                          .WithReferences(references)
                                                                          .AddImports(
                                                                          "System",
                                                                          "System.Windows",
                                                                          "System.Windows.Input",
                                                                          "System.Windows.Controls",
                                                                          "System.Windows.Media.Imaging",
                                                                          "System.Windows.Media.Animation",
                                                                          "System.Windows.Media",
                                                                          "System.Windows.Markup",
                                                                          "System.Threading.Tasks",
                                                                          "System.Windows.Threading",
                                                                          "System.IO",
                                                                          "System.Reflection",
                                                                          "System.Runtime.InteropServices",
                                                                          "System.Windows.Interop"
                                                                          ));
            return script.RunAsync().Result;
        }
        private string GenerateStoryCode(SceneInfo? start_scene=null)
        {
            var IsScript = start_scene is not null;
            var start_scene_flag = !IsScript;
            var stories = new StringBuilder();
            var end_func = $$"""
                IsHandling = true;
                {{(IsScript ? "End();" : "var b = window.End();\nb.Completed += (sender, e) => {\r\n    IsHandling = false;  \r\n};\r\n        MoviePlayer.Source = null;\r\n        _movieFile.Dispose();")}}
                """;
            var episode_counter = 0;
            var scene_counter = 0;
            var script_counter = 0;
            var story_ref = story.StoryInfos.Length;
            var msw = "";
            var pre_content = new ContentInfo();
            foreach (var t in story.StoryInfos)
            {
                episode_counter++;
                var isLastEpisode = episode_counter == story_ref;
                var premsw = -1;
                var pre_scene = new SceneInfo();
                if (string.IsNullOrEmpty(t.path))
                {
                    episode_counter--;
                    story_ref--;
                }
                else if (t.IsScenes())
                {
                    var episode = emanager.GetEpisode(t.path);
                    if (!start_scene_flag && !episode.smanager.scenes.Values.Contains(start_scene))
                    {
                        episode_counter--;
                        story_ref--;
                        continue;
                    }
                    var scene_ref = episode.smanager.scenes.Count;
                    switch (pre_content.trans)
                    {
                        case TransitionTypes.NONE:
                            stories.Append($$"""
                            public async void Episode{{episode_counter}}() {
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                MoviePlayer.Source = null;
                                await Task.Delay({{pre_content.interval * 1000}});
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                Scene{{scene_counter + 1}}();
                            }
                            """);
                            break;
                        case TransitionTypes.SIMPLE:
                            stories.Append($$"""
                            public async void Episode{{episode_counter}}() {
                                IsHandling = true;
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                Transition.Fill = MainWindow.GetBrush(@"{{pre_content.trans_color}}");
                                var fadeout = new DoubleAnimation()
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadeout}}))
                                };
                                var b1 = new Storyboard();
                                Storyboard.SetTarget(fadeout, Transition);
                                Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                                b1.Children.Add(fadeout);
                                var fadein = new DoubleAnimation()
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadein}}))
                                };
                                var b2 = new Storyboard();
                                Storyboard.SetTarget(fadein, Transition);
                                Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                                b2.Children.Add(fadein);
                                b1.Completed += async (sender, e) => {
                                    MoviePlayer.Source = null;
                                    await Task.Delay({{pre_content.interval * 1000}});
                                    b2.Begin();
                                };
                                b1.Begin();
                                b2.Completed += async (sender, e) => {
                                    IsHandling = false;
                                    {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                    Scene{{scene_counter + 1}}();
                                };
                            }
                            """);
                            break;
                    }
                    foreach (var s in episode.smanager.scenes)
                    {
                        if (start_scene_flag)
                        {

                        }
                        else if (s.Value == start_scene)
                        {
                            start_scene_flag = true;
                        }
                        else
                        {
                            scene_ref--;
                            continue;
                        }
                        scene_counter++;
                        var start_script_num = script_counter + 1;
                        var isLastScene = scene_ref <= scene_counter;
                        ScriptInfo? pre_script = null;
                        foreach (var script in s.Value.scripts)
                        {
                            script_counter++;
                            var isLastScript = s.Value.scripts.Count <= (script_counter - start_script_num + 1);
                            var i = mmanager.windows[s.Value.msw].Interval;
                            var text = Utils.GetEscapedString(script.script);
                            var speaker = string.IsNullOrEmpty(script.speaker)
                                          ? "NamePlate.Visibility = Visibility.Hidden;"
                                          : $"""
                                          Speaker.Content = @"{script.speaker}";
                                          NamePlate.Visibility = Visibility.Visible;
                                          """;
                            var charas = new StringBuilder();
                            if (pre_script is null || !Utils.IsEqualList(pre_script.charas, script.charas))
                            {
                                charas = charas.AppendLine("RemoveCharas();");
                                if (0 < script.charas.Count)
                                {
                                    var per_x = MainWindow.pmanager.Project.Size[0] / (script.charas.Count * 2);
                                    var j = 1;
                                    charas.AppendLine("BitmapImage c_bmp;");
                                    foreach (var c in script.charas)
                                    {
                                        if (!string.IsNullOrEmpty(c))
                                        {
                                            var file = ConvertPath(Path.Combine("Characters", c));
                                            charas.AppendLine($$"""
                                        c_bmp = MainWindow.CreateBmp(MainWindow.GetResource(@"{{file}}"));
                                        SetCharacter(new Image() {
                                            Source = c_bmp,
                                            Stretch = Stretch.Uniform,
                                            Height = {{MainWindow.pmanager.Project.Size[1]}}
                                        }, {{per_x * (j * 2 - 1)}} - c_bmp.Width  / 4);
                                        """);
                                            j++;
                                        }
                                    }
                                }
                            }
                            if (isLastScene && isLastScript)
                            {
                                stories.Append($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            CurrentScript = "{{text}}";
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = (sender, e) => {
                                if (IsHandling) return;
                                else if (MessageWindowCanvas.Visibility == Visibility.Visible){
                                    if (IsNowScripting) {
                                        terminate_scripting = true;
                                    }
                                    else{
                                        FinishBgm();
                                        {{(isLastEpisode ? end_func : $"Episode{episode_counter + 1}();")}}
                                    }
                                }
                                else {
                                    MessageWindowCanvas.Visibility = Visibility.Visible;
                                }
                            };
                            MainPanel.MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            foreach (var s in "{{text}}") {
                                if (terminate_scripting) {
                                    Script.Text = "{{text}}";
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{i}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            else if (isLastScript)
                            {
                                stories.Append($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            CurrentScript = "{{text}}";
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = (sender, e) => {
                                if (IsHandling) return;
                                else if (MessageWindowCanvas.Visibility == Visibility.Visible) {
                                    if (IsNowScripting) {
                                        terminate_scripting = true;
                                    }
                                    else {
                                        Scene{{scene_counter + 1}}();
                                    }
                                }
                                else {
                                    MessageWindowCanvas.Visibility = Visibility.Visible;
                                }
                            };
                            MainPanel.MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            foreach (var s in "{{text}}") {
                                if (terminate_scripting) {
                                    Script.Text = "{{text}}";
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{i}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            else
                            {
                                stories.Append($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            CurrentScript = "{{text}}";
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = (sender, e) => {
                                if (IsHandling) return;
                                else if (MessageWindowCanvas.Visibility == Visibility.Visible) {
                                    if (IsNowScripting) {
                                        terminate_scripting = true;
                                    }
                                    else {
                                        ShowScript{{script_counter + 1}}();
                                    }
                                }
                                else {
                                    MessageWindowCanvas.Visibility = Visibility.Visible;
                                }
                            };
                            MainPanel.MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            foreach (var s in "{{text}}") {
                                if (terminate_scripting) {
                                    Script.Text = "{{text}}";
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{i}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            pre_script = script;
                        }
                        var width = mmanager.windows[s.Value.msw].Width;
                        var height = mmanager.windows[s.Value.msw].Height;
                        var msw_bg_color = mmanager.windows[s.Value.msw].BgColor;
                        var alpha = mmanager.windows[s.Value.msw].BgColorAlpha;
                        var bgalpha = mmanager.windows[s.Value.msw].BgAlpha;
                        string bg, bgm, condition;
                        bg = "Bg.Source = null;\r\n";
                        if (string.IsNullOrEmpty(s.Value.bg))
                        {
                            condition = "false";
                        }
                        else
                        {
                            bg += $$"""
                                    Bg.Source = MainWindow.CreateBmp(MainWindow.GetResource(@"{{ConvertPath(s.Value.bg)}}"));
                                    """;
                            condition = "Bg.ActualWidth == 0";
                        }
                        if (pre_scene.bgm == s.Value.bgm)
                        {
                            bgm = "";
                        }
                        else if (string.IsNullOrEmpty(s.Value.bgm))
                        {
                            bgm = "FinishBgm();";
                        }
                        else
                        {
                            bgm = $$"""PlayBgm(MainWindow.GetResource(@"{{ConvertPath(s.Value.bgm)}}"));""";
                        }
                        var interval = 0 < pre_scene.interval ? $"""
                            await Task.Delay({pre_scene.interval * 1000});
                            """ : "";
                        var window = mmanager.windows[s.Value.msw];
                        var msw_bg = window.Bg;
                        var msw_img = msw_bg.Length > 0 ? $"MessageWindowBg.Source = MainWindow.CreateBmp(MainWindow.GetResource(@\"{ConvertPath(msw_bg)}\"))" : "";
                        var plate_img = string.IsNullOrEmpty(window.NamePlateBgImage) ? "" : $"NamePlateBgImage.Source = MainWindow.CreateBmp(MainWindow.GetResource(@\"{ConvertPath(window.NamePlateBgImage)}\"));";
                        var fontsize = window.FontSize;
                        var font = window.Font;
                        var textcolor = window.TextColor;
                        msw = $"""
                                    Script.Text = "";
                                    {bg}
                                    {msw_img};
                                    MessageWindow.Width = {width};
                                    MessageWindow.Height = {height};
                                    MessageWindowBg.Width = {width};
                                    MessageWindowBg.Height = {height};
                                    MessageWindow.Background = MainWindow.GetBrush(@"{msw_bg_color}");
                                    MessageWindow.Background.Opacity = {alpha};
                                    MessageWindowBg.Opacity = {bgalpha};
                                    Canvas.SetLeft(Script, {window.ScriptLeftPos});
                                    Canvas.SetTop(Script, {window.ScriptTopPos});
                                    Script.Width = {window.ScriptWidth};
                                    Script.FontSize = {fontsize};
                                    Script.FontFamily = new FontFamily(@"{font}");
                                    Script.Foreground = MainWindow.GetBrush(@"{textcolor}");
                                    Canvas.SetLeft(NamePlate, {window.NamePlateLeftPos});
                                    Canvas.SetBottom(NamePlate, {window.NamePlateBottomPos});
                                    MessageWindow.Width = {window.Width};
                                    MessageWindow.Height = {window.Height};
                                    NamePlate.Width = {window.NamePlateWidth};
                                    NamePlate.Height = {window.NamePlateHeight};
                                    NamePlate.Background = MainWindow.GetBrush(@"{window.NamePlateBgColor}");
                                    NamePlate.Background.Opacity = {window.NamePlateBgColorAlpha};
                                    {plate_img}
                                    NamePlateBgImage.Opacity = {window.NamePlateBgImageAlpha};
                                    Speaker.Foreground = MainWindow.GetBrush(@"{window.NameFontColor}");
                                    Speaker.FontFamily = new FontFamily(@"{window.NameFont}");
                                    Speaker.FontSize = {window.NameFontSize};
                                    Canvas.SetLeft(MessageWindow, {window.WindowLeftPos});
                                    Canvas.SetBottom(MessageWindow,{window.WindowBottom});
                                    {interval}
                                """;
                        if (premsw == s.Value.msw)
                        {
                            switch (pre_scene.trans)
                            {
                                case TransitionTypes.NONE:
                                    stories.Append($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        {{interval}}
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        {{bg}}
                        {{bgm}}
                        IsHandling = false;
                        if (script == -1)
                        {
                            ShowScript{{start_script_num}}();
                        }
                        else
                        {
                            {{msw}}
                            var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                            f.Invoke(this, null);
                        }
                    }
                    """);
                                    break;
                                case TransitionTypes.SIMPLE:
                                    stories.Append($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        var bg_loaded = false;
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        if (script == -1)
                        {
                            Transition.Fill = MainWindow.GetBrush(@"{{pre_scene.trans_color}}");
                            var fadeout = new DoubleAnimation()
                            {
                                From = 0,
                                To = 1,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadeout}}))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadeout, Transition);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadeout);
                            b1.Completed += async (sender, e) => {
                                {{bg}}
                                CharacterPictures.Children.Clear();
                                while ({{condition}}) {
                                    await Task.Delay(500);
                                }
                                {{interval}}
                                bg_loaded = true;
                            };
                            b1.Begin();
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadein}}))
                            };
                            var b2 = new Storyboard();
                            Storyboard.SetTarget(fadein, Transition);
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b2.Children.Add(fadein);
                            b2.Completed += async (sender, e) => {
                                {{bgm}}
                                IsHandling = false;
                                ShowScript{{start_script_num}}();
                            };
                            while (!bg_loaded){
                                await Task.Delay(500);
                            }
                            b2.Begin();
                        }
                        else
                        {
                            {{bg}}
                            CharacterPictures.Children.Clear();
                            while ({{condition}}) {
                                await Task.Delay(500);
                            }
                            {{msw}}
                            {{interval}}
                            bg_loaded = true;
                            {{bgm}}
                            IsHandling = false;
                            var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                            f.Invoke(this, null);
                        }
                    }
                    """);
                                    break;
                        }
                        }
                        else
                        {
                            switch (pre_scene.trans)
                            {
                                case TransitionTypes.NONE:
                                    stories.Append($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        CurrentScene = {{scene_counter}};
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        {{msw}}
                        {{bgm}}
                        IsHandling = false;
                        if (script == -1)
                        {
                            ShowScript{{start_script_num}}();
                        }
                        else
                        {
                            var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                            f.Invoke(this, null);
                        }
                    }
                    """);
                                    break;
                                case TransitionTypes.SIMPLE:
                                    stories.Append($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        if (script == -1)
                        {
                            Transition.Fill = MainWindow.GetBrush(@"{{pre_scene.trans_color}}");
                            var fadeout = new DoubleAnimation()
                            {
                                From = 0,
                                To = 1,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadeout}}))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadeout, Transition);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadeout);
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadein}}))
                            };
                            var b2 = new Storyboard();
                            Storyboard.SetTarget(fadein, Transition);
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b2.Children.Add(fadein);
                            b2.Completed += (sender, e) => {
                                {{bgm}}
                                IsHandling = false;
                                if (script == -1)
                                {
                                    ShowScript{{start_script_num}}();
                                }
                                else
                                {
                                    ShowScript{{start_script_num}}();
                                }
                            };
                            b1.Completed += async (sender, e) => {
                                {{msw}}
                                b2.Begin();
                            };
                            b1.Begin();
                        }
                        else
                        {
                            {{msw}}
                            {{bgm}}
                            IsHandling = false;
                            var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                            f.Invoke(this, null);
                            }
                        }
                    }
                    """);
                                    break;
                            }
                        }
                        premsw = s.Value.msw;
                        pre_scene = s.Value;
                    }
                }
                else
                {
                    if (!start_scene_flag)
                    {
                        episode_counter--;
                        story_ref--;
                        continue;
                    }
                    switch (pre_content.trans)
                    {
                        case TransitionTypes.NONE:
                            stories.Append($$"""
                            public async void Episode{{episode_counter}}() {
                                IsHandling = true;
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                                {{(IsScript ? $"MoviePlayer.Source = new Uri(MainWindow.GetResource(@\"{t.GetRelPath()}\"));" : $"    _movieFile = new TempFile(@\"{Path.GetExtension(t.path)}\");\r\n    _movieFile.Write(MainWindow.GetResource(@\"{ConvertPath(t.GetRelPath())}\"));\r\n    MoviePlayer.Source = new Uri(_movieFile.path);")}}
                                if (MediaEnded is not null) {
                                    MoviePlayer.MediaEnded -= MediaEnded;
                                }
                                MediaEnded = (sender, e) => {
                                    IsHandling = false;
                                    {{(isLastEpisode ? end_func : $"Episode{episode_counter + 1}();")}}
                                };
                                Bg.Source = null;
                                CharacterPictures.Children.Clear();
                                MessageWindowCanvas.Visibility = Visibility.Collapsed;
                                MoviePlayer.MediaEnded += MediaEnded;
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                                await Task.Delay({{pre_content.interval * 1000}});
                                MoviePlayer.Play();
                            }
                            """);
                            break;
                        case TransitionTypes.SIMPLE:
                            stories.Append($$"""
                            public async void Episode{{episode_counter}}() {
                                IsHandling = true;
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                Transition.Fill = MainWindow.GetBrush(@"{{pre_content.trans_color}}");
                                var fadeout = new DoubleAnimation()
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadeout}}))
                                };
                                var b1 = new Storyboard();
                                Storyboard.SetTarget(fadeout, Transition);
                                Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                                b1.Children.Add(fadeout);
                                var fadein = new DoubleAnimation()
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadein}}))
                                };
                                var b2 = new Storyboard();
                                Storyboard.SetTarget(fadein, Transition);
                                Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                                b2.Children.Add(fadein);
                                b2.Completed += async (sender, e) => {
                                    MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                                    {{(IsScript ? $"MoviePlayer.Source = new Uri(MainWindow.GetResource(@\"{t.GetRelPath()}\"));" : $"    _movieFile = new TempFile(@\"{Path.GetExtension(t.path)}\");\r\n    _movieFile.Write(MainWindow.GetResource(@\"{ConvertPath(t.GetRelPath())}\"));\r\n    MoviePlayer.Source = new Uri(_movieFile.path);")}}
                                    if (MediaEnded is not null) {
                                        MoviePlayer.MediaEnded -= MediaEnded;
                                    }
                                    MediaEnded = (sender, e) => {
                                        IsHandling = false;
                                        {{(isLastEpisode ? end_func : $"Episode{episode_counter + 1}();")}}
                                    };
                                    MoviePlayer.MediaEnded += MediaEnded;
                                    MoviePlayer.Play();
                                };
                                b1.Completed += async (sender, e) => {
                                    await Task.Delay({{pre_content.interval * 1000}});
                                    Bg.Source = null;
                                    MoviePlayer.Source = null;
                                    CharacterPictures.Children.Clear();
                                    MessageWindowCanvas.Visibility = Visibility.Collapsed;
                                    {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                                    b2.Begin();
                                };
                                b1.Begin();
                            }
                            """);
                            break;
                    }
                }

                pre_content = t;
            }
            return stories.ToString();
        }

        private string GenerateScriptCode(SceneInfo start)
        {
            return $$"""
                public class MainWindow : Window
                {
                    private ScaleTransform Scale;
                    private Menu WindowMenu;
                    private MenuItem MaximizeMenu;
                    private DockPanel MainPanel;
                    private Grid MainGrid;
                    private Image Bg;
                    private Canvas CharacterPictures;
                    private Canvas MessageWindowCanvas;
                    private Canvas NamePlate;
                    private Image NamePlateBgImage;
                    private Label Speaker;
                    private StackPanel MessageWindow;
                    private Image MessageWindowBg;
                    private TextBlock Script;
                    private MediaElement MoviePlayer;
                    private System.Windows.Shapes.Rectangle Transition;
                    private bool IsHandling;
                    private bool IsNowScripting;
                    private MouseButtonEventHandler OnMouseLeftDown = (sender, e) => {};
                    private RoutedEventHandler MediaEnded;
                    private RoutedEventHandler OnMediaFinished;
                    private MediaElement BgmPlayer;
                    private DispatcherTimer TransitionTimer;
                    private bool fin;
                    public int CurrentScriptId, CurrentScene;
                    private string CurrentScript;

                    public static string GetResource(params string[] names)
                    {
                        var res = @"{{MainWindow.pmanager.ProjectResourceDir}}";
                        foreach (var f in names)
                        {
                            res = Path.Combine(res, f);
                        }
                        return res;
                    }
                
                    public static BitmapImage CreateBmp(string path)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(path);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                
                    public static BitmapImage CreateBmp(Stream stream)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = stream;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }

                    public static BitmapImage CreateBmp(byte[] b)
                    {
                        using (var m = new MemoryStream(b))
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = m;
                            bmp.EndInit();
                            bmp.Freeze();
                            return bmp;
                        }
                    }

                    public static SolidColorBrush GetBrush(string code)
                    {
                        return (SolidColorBrush)new BrushConverter().ConvertFromString(code);
                    }
                
                    public MainWindow()
                    {
                        Title = @"{{MainWindow.pmanager.Project.Title}}";
                        Width = {{MainWindow.pmanager.Project.Size[0]}};
                        Height = {{MainWindow.pmanager.Project.Size[1]}};
                        ResizeMode = ResizeMode.CanMinimize;
                        Background = Brushes.Black;
                        Closing += (sender, e) => {
                            fin = true;
                            if (BgmPlayer is not null) {
                                BgmPlayer.Stop();
                            }
                        };
                        MouseMove += (sender, e) => {
                            if (e.GetPosition(MainPanel).Y <= 20 || IsMenuOpening()) {
                                WindowMenu.Visibility = Visibility.Visible;
                            }
                            else {
                                WindowMenu.Visibility = Visibility.Collapsed;
                            }
                        };
                        Start();
                    }

                    private void Transform()
                    {
                        var r = Math.Min(ActualWidth / {{MainWindow.pmanager.Project.Size[0]}}, ActualHeight / {{MainWindow.pmanager.Project.Size[1]}});
                        Scale.ScaleX = r;
                        Scale.ScaleY = r;
                    }

                    private bool IsMenuOpening()
                    {
                        foreach (MenuItem i in WindowMenu.Items) {
                            if (i.IsSubmenuOpen) {
                                return true;
                            }
                        }
                        return false;
                    }

                    private void SetCharacter(Image chara, double x)
                    {
                        chara.Opacity = 0;
                        Canvas.SetLeft(chara, x);
                        CharacterPictures.Children.Add(chara);
                        var b = new DoubleAnimation() {
                            From = 0.0,
                            To = 1.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(300))
                        };
                        chara.BeginAnimation(UIElement.OpacityProperty, b);
                    }

                    private void RemoveCharas()
                    {
                        foreach (UIElement c in CharacterPictures.Children)
                        {
                            var b = new DoubleAnimation() {
                                From = 1.0,
                                To = 0.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(300))
                            };
                            b.Completed += (sender, e) => {
                                CharacterPictures.Children.Remove(c);
                            };
                            c.BeginAnimation(UIElement.OpacityProperty, b);
                        }
                    }
                    

                    private void PlayBgm(string bgm)
                    {
                        var b = new DoubleAnimation() {
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                            FillBehavior = FillBehavior.Stop
                        };
                        b.Completed += (sender, e) => {
                            BgmPlayer.Pause();
                            BgmPlayer.Source = new Uri(bgm);
                            BgmPlayer.Play();
                        };
                        BgmPlayer.BeginAnimation(MediaElement.VolumeProperty, b);
                    }

                    private void FinishBgm()
                    {
                        var b = new DoubleAnimation() {
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                        };
                        b.Completed += (sender, e) => {
                            BgmPlayer.Stop();
                        };
                        BgmPlayer.BeginAnimation(MediaElement.VolumeProperty, b);
                    }
                
                    public void Start() {
                        var xaml = (FrameworkElement)XamlReader.Parse({{$"\"\"\"\n{GenerateGameUIXaml()}\n\"\"\""}});
                        Content = xaml;
                        Scale = (ScaleTransform)xaml.FindName("Scale");
                        WindowMenu = (Menu)xaml.FindName("WindowMenu");
                        MaximizeMenu = (MenuItem)xaml.FindName("MaximizeMenu");
                        MaximizeMenu.Click += (sender, e) => {
                            if (MaximizeMenu.IsChecked) {
                                WindowStyle = WindowStyle.None;
                                WindowState = WindowState.Maximized;
                                Transform();
                            }
                            else {
                                WindowStyle = WindowStyle.SingleBorderWindow;
                                WindowState = WindowState.Normal;
                                Transform();
                            }
                        };
                        MainPanel = (DockPanel)xaml.FindName("MainPanel");
                        MainGrid = (Grid)xaml.FindName("MainGrid");
                        Bg = (Image)xaml.FindName("Bg");
                        CharacterPictures = (Canvas)xaml.FindName("CharacterPictures");
                        MessageWindowCanvas = (Canvas)xaml.FindName("MessageWindowCanvas");
                        NamePlate = (Canvas)xaml.FindName("NamePlate");
                        NamePlateBgImage = (Image)xaml.FindName("NamePlateBgImage");
                        Speaker = (Label)xaml.FindName("Speaker");
                        MessageWindow = (StackPanel)xaml.FindName("MessageWindow");
                        MessageWindowBg = (Image)xaml.FindName("MessageWindowBg");
                        Script = (TextBlock)xaml.FindName("Script");
                        MoviePlayer = (MediaElement)xaml.FindName("MoviePlayer");
                        Transition = (System.Windows.Shapes.Rectangle)xaml.FindName("Transition");
                        MainPanel.MouseRightButtonDown += (sender, e) => {
                            if (!IsHandling) MessageWindowCanvas.Visibility = MessageWindowCanvas.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                        };
                        BgmPlayer = new() {
                            Visibility = Visibility.Collapsed,
                            UnloadedBehavior = MediaState.Manual,
                            Volume = 0.5
                        };
                        BgmPlayer.MediaEnded += (sender, e) => {
                            BgmPlayer.Position = TimeSpan.Zero;
                            BgmPlayer.Play();
                        };
                        Episode1();
                    }

                    private void End()
                    {
                    
                    }
                   {{GenerateStoryCode(start)}}
                }
                var window = new MainWindow(); 
                window.ShowDialog();
            """;
        }

        private string GenerateCompilationCode()
        {
            var stories = GenerateStoryCode();
            return $$"""
                using Game.Core;
                using NAudio.Wave;
                using System.Windows;
                using System.Windows.Input;
                using System.Windows.Controls;
                using System.Windows.Media.Imaging;
                using System.Windows.Media;
                using System.Windows.Media.Animation;
                using System.Threading.Tasks;
                using System.Windows.Threading;
                using System.IO;
                using System.Text;
                using System.Text.Json;
                using System.Linq;
                using System;

                namespace Game {
                    partial class MainWindow : Window
                    {
                        private static Dictionary<string, object[]> _table;
                        private TitlePage TitlePage;
                        private GamePage GamePage;
                        private SaveDataPage SaveDataPage;
                        private bool fin;
                
                        public static byte[] GetResource(string h)
                        {
                            var i = _table[h];
                            var l = ((JsonElement)i[0]).GetInt32();
                            var b = new byte[l];
                            using (var f = new FileStream(Path.Combine("datas", ((JsonElement)i[2]).GetString()), FileMode.Open, FileAccess.Read))
                            {
                                f.Seek(((JsonElement)i[1]).GetInt32(), SeekOrigin.Begin);
                                f.Read(b, 0, l);
                            }
                            b[0] = 40;
                            b[1] = 181;
                            using (var d = new ZstdNet.Decompressor())
                            {
                                return d.Unwrap(b);
                            }
                        }
                    
                        public static BitmapImage CreateBmp(string path)
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(path);
                            bmp.EndInit();
                            bmp.Freeze();
                            return bmp;
                        }
                    
                        public static BitmapImage CreateBmp(Stream stream)
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = stream;
                            bmp.EndInit();
                            bmp.Freeze();
                            return bmp;
                        }

                        public static BitmapImage CreateBmp(byte[] b)
                        {
                            using (var m = new MemoryStream(b))
                            {
                                var bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.StreamSource = m;
                                bmp.EndInit();
                                bmp.Freeze();
                                return bmp;
                            }
                        }
                
                        public static SolidColorBrush GetBrush(string code)
                        {
                            return (SolidColorBrush)new BrushConverter().ConvertFromString(code);
                        }

                        public static void PlaySound(string h)
                        {
                            var player = new WaveOutEvent();
                            var s = new MemoryStream(GetResource(h));
                            try
                            {
                                 player.Init(new WaveFileReader(s));   
                            }
                            catch
                            {
                                try
                                {
                                    player.Init(new Mp3FileReader(s));
                                }
                                catch
                                {
                                    player.Init(new AiffFileReader(s));
                                }
                            }
                            player.Play();
                        }
                    
                        public MainWindow()
                        {
                            Title = @"{{MainWindow.pmanager.Project.Title}}";
                            Width = {{MainWindow.pmanager.Project.Size[0]}};
                            Height = {{MainWindow.pmanager.Project.Size[1]}};
                            ResizeMode = ResizeMode.CanMinimize;
                            Background = Brushes.Black;
                            TitlePage = new(this);
                            GamePage = new(this);
                            Closing += (sender, e) => {
                                var res = MessageBox.Show("終了しますか？", @"{{MainWindow.pmanager.ProjectName}}", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                e.Cancel = res is not MessageBoxResult.Yes;
                                if (!e.Cancel)
                                {
                                    fin = true;
                                    GamePage.Clear();
                                }
                            };
                            MouseMove += (sender, e) => {
                                if (e.GetPosition(this).Y <= 20 || IsMenuOpening()) {
                                    WindowMenu.Visibility = Visibility.Visible;
                                }
                                else {
                                    WindowMenu.Visibility = Visibility.Collapsed;
                                }
                            };
                            Loaded += (sender, e) => {
                                MaximizeMenu.Click += (sender, e) => {
                                    if (MaximizeMenu.IsChecked) {
                                        WindowStyle = WindowStyle.None;
                                        WindowState = WindowState.Maximized;
                                        Transform();
                                    }
                                    else {
                                        WindowStyle = WindowStyle.SingleBorderWindow;
                                        WindowState = WindowState.Normal;
                                        Transform();
                                    }
                                };
                                SaveDataPage = new(GamePage, Screen);
                                ShowTitlePage();
                            };
                            if (TitlePage.FindName("StartButton") is Button sbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                sbtn.MouseEnter += (sender, e) => {
                                    PlaySound(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
                                };
                                """)}}
                                sbtn.Click += (sender, e) => {
                                var b1 = new DoubleAnimation() {
                                    From = 1.0,
                                    To = 0.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(1000))
                                };
                                var b2 = new DoubleAnimation() {
                                    From = 0.0,
                                    To = 1.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(1000))
                                };
                                b2.Completed += (sender, e) => {
                                    GamePage.Episode1();
                                };
                                b1.Completed += (sender, e) => {
                                    Screen.Navigate(GamePage);
                                    Screen.BeginAnimation(UIElement.OpacityProperty, b2);
                                };
                                sbtn.IsEnabled = false;
                                TitlePage.Loaded += (sender, e) => {
                                    sbtn.IsEnabled = true;
                                };
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlaySound(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                Screen.BeginAnimation(UIElement.OpacityProperty, b1);
                                };
                            }
                            if (TitlePage.FindName("LoadButton") is Button lbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                lbtn.MouseEnter += (sender, e) => {
                                    PlaySound(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
                                };
                                """)}}
                                lbtn.Click += (sender, e) => {
                                    {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlaySound(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                    OpenLoadDialog();
                                };
                            }
                            if (TitlePage.FindName("FinishButton") is Button fbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                fbtn.MouseEnter += (sender, e) => {
                                    PlaySound(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
                                };
                                """)}}
                                fbtn.Click += (sender, e) => {
                                    {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlaySound(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                    Close();
                                };
                            }
                            _table = JsonSerializer.Deserialize<Dictionary<string, object[]>>(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine("datas", "data.dat"))
                            .Select(c => (byte)~c)
                            .ToArray()));
                        }

                        private void Transform()
                        {
                            var r = Math.Min(ActualWidth / {{MainWindow.pmanager.Project.Size[0]}}, ActualHeight / {{MainWindow.pmanager.Project.Size[1]}});
                            Scale.ScaleX = r;
                            Scale.ScaleY = r;
                        }
                
                        private bool IsMenuOpening()
                        {
                            foreach (MenuItem i in WindowMenu.Items) {
                                if (i.IsSubmenuOpen) {
                                    return true;
                                }
                            }
                            return false;
                        }

                        public void ShowTitlePage()
                        {
                            Screen.Navigate(TitlePage);
                        }

                        public DoubleAnimation End()
                        {
                            var b1 = new DoubleAnimation() {
                                From = 1.0,
                                To = 0.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(5000))
                            };
                            var b2 = new DoubleAnimation() {
                                From = 0.0,
                                To = 1.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(1000))
                            };
                            b1.Completed += async (sender, e) => {
                                await Task.Delay(1000);
                                ShowTitlePage();
                                GamePage.Clear();
                                Screen.BeginAnimation(UIElement.OpacityProperty, b2);
                            };
                            GamePage.MessageWindowCanvas.Visibility = Visibility.Hidden;
                            Screen.BeginAnimation(UIElement.OpacityProperty, b1);
                            return b2;
                        }

                        private void OpenSaveDialog(object sender, RoutedEventArgs e)
                        {
                            SaveDataPage.IsSaveMode = true;
                            Screen.Navigate(SaveDataPage);
                        }

                        private void OpenLoadDialog(object sender, RoutedEventArgs e)
                        {
                            OpenLoadDialog();
                        }

                        public void OpenLoadDialog()
                        {
                            SaveDataPage.IsSaveMode = false;
                            Screen.Navigate(SaveDataPage);
                        }
                    }
                }
                """;
        }
    }
    class FilePackingData
    {
        public int FinishedCount, Length;
        public string FileName;
    }
    public class CompilationFiles
    {
        public string baseDir, main, main_xaml, app, app_xaml, csproj, gamepage, gamepage_xaml, title, title_xaml, savedata, savedata_xaml, savedatamanager;

        public CompilationFiles(string dir)
        {
            baseDir = dir;
            main = Path.Combine(baseDir, "MainWindow.xaml.cs");
            main_xaml = Path.Combine(baseDir, "MainWindow.xaml");
            app = Path.Combine(baseDir, "App.xaml.cs");
            app_xaml = Path.Combine(baseDir, "App.xaml");
            csproj = Path.Combine(baseDir, $"Game.csproj");
            gamepage = Path.Combine(baseDir, "GamePage.xaml.cs");
            gamepage_xaml = Path.Combine(baseDir, "GamePage.xaml");
            title = Path.Combine(baseDir, "Title.xaml.cs");
            title_xaml = Path.Combine(baseDir, "Title.xaml");
            savedata = Path.Combine(baseDir, "SaveDataPage.xaml.cs");
            savedata_xaml = Path.Combine(baseDir, "SaveDataPage.xaml");
            savedatamanager = Path.Combine(baseDir, "SaveDataManager.cs");
        }
    }
}
