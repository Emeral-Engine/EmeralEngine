using EmeralEngine.Core;
using EmeralEngine.MessageWindow;
using EmeralEngine.Notify;
using EmeralEngine.Project;
using EmeralEngine.Resource.CustomTransition;
using EmeralEngine.Scene;
using EmeralEngine.Script;
using EmeralEngine.Story;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Builder
{
    class GameBuilder
    {
        private string[] SCALING_MODE = { "Linear", "NearestNeighbor" };
        public const string DOTNET_DIR = "dotnet";
        private static Regex NewLinePat = new Regex(@"(?<!\\)\\n");
        private string title, projfile;
        private MessageWindowManager mmanager;
        private StoryManager story;
        private EpisodeManager emanager;
        private Assembly[] references;
        private Dictionary<string, string> hashTable;
        private string ScalingMode
        {
            get => SCALING_MODE[MainWindow.pmanager.Project.ScalingMode];
        }

        public GameBuilder(string title, string proj, Assembly[] refs, MessageWindowManager m, StoryManager s, EpisodeManager e)
        {
            this.title = title;
            projfile = proj;
            mmanager = m;
            story = s;
            emanager = e;
            references = refs;
        }

        public string CheckDotNetSDK()
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--list-sdks",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                foreach (var line in output.Split('\n'))
                {
                    if (line.Trim().StartsWith("10.")) return "";
                }
                return ".NET SDKは見つかりましたが、バージョン10が見つかりません";
            }
            catch
            {
                return ".NET SDKの存在を確認できませんでした";
            }
        }

        public void ExportData(string dst)
        {
            var num = 1;
            var data = new StringBuilder(NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.BeginChar, "\n"));
            for (var i=0; i< story.StoryInfos.Length; i++)
            {
                var t = story.StoryInfos[i];
                if (t.IsScenes())
                {
                    var scenes = new StringBuilder();
                    var episode = emanager.GetEpisode(t.FullPath);
                    var scount = 0;
                    foreach (var s in episode.smanager.scenes)
                    {
                        scount++;
                        var scripts = new StringBuilder();
                        for (var j=0; j<s.Value.scripts.Count; j++)
                        {
                            var sc = s.Value.scripts[j];
                            var script = MainWindow.pmanager.Project.ExportSettings.ScriptFormat;
                            var charas = new StringBuilder();
                            for (var k = 0; k < sc.charas.Count; k++)
                            {
                                var chara = MainWindow.pmanager.Project.ExportSettings.PictureFormat;
                                chara = Regex.Replace(chara, @"(?<!\\)%\(n4\)", (k + 1).ToString());
                                chara = Regex.Replace(chara, @"(?<!\\)%\(picture\)", sc.charas[k]);
                                chara = Regex.Replace(chara, @"(?<!\\)%\(picturesn\)", sc.charas.Count.ToString());
                                charas.Append(HandleString(chara));
                                if (k + 1 < sc.charas.Count)
                                {
                                    charas.Append(NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.PicturesSeparator, "\n"));
                                }
                            }
                            script = Regex.Replace(script, @"(?<!\\)%\(n1\)", num.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(n2\)", s.Value.order.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(bg\)", HandleString(s.Value.bg));
                            script = Regex.Replace(script, @"(?<!\\)%\(bgm\)", HandleString(s.Value.bgm));
                            script = Regex.Replace(script, @"(?<!\\)%\(fadeout\)", s.Value.fadeout.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(fadein\)", s.Value.fadein.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(wait\)", s.Value.interval.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(n3\)", (j+1).ToString());
                            script = IndentedString(script, "pictures", charas.ToString());
                            script = Regex.Replace(script, @"(?<!\\)%\(speaker\)", HandleString(sc.speaker ?? ""));
                            script = IndentedString(script, "script", HandleString(sc.script ?? ""));
                            if (j+1 < s.Value.scripts.Count)
                            {
                                script += NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.ScriptsSeparator, "\n");
                            }
                            scripts.Append(script);
                        }
                        var scene = MainWindow.pmanager.Project.ExportSettings.SceneFormat;
                        scene = Regex.Replace(scene, @"(?<!\\)%\(n1\)", num.ToString());
                        scene = Regex.Replace(scene, @"(?<!\\)%\(n2\)", s.Value.order.ToString());
                        scene = Regex.Replace(scene, @"(?<!\\)%\(bg\)", HandleString(s.Value.bg));
                        scene = Regex.Replace(scene, @"(?<!\\)%\(bgm\)", HandleString(s.Value.bgm));
                        scene = Regex.Replace(scene, @"(?<!\\)%\(fadeout\)", s.Value.fadeout.ToString());
                        scene = Regex.Replace(scene, @"(?<!\\)%\(fadein\)", s.Value.fadein.ToString());
                        scene = Regex.Replace(scene, @"(?<!\\)%\(wait\)", s.Value.interval.ToString());
                        scene = IndentedString(scene, "scripts", scripts.ToString());
                        if (scount < episode.smanager.scenes.Count)
                        {
                            scene += NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.ScenesSeparator, "\n");
                        }
                        scenes.Append(scene);
                    }
                    var d = MainWindow.pmanager.Project.ExportSettings.ContentFormat;
                    d = Regex.Replace(d, @"(?<!\\)%\(n1\)", num.ToString());
                    d = Regex.Replace(d, @"(?<!\\)%\(epname\)", episode.Name);
                    d = IndentedString(d, "scenes", scenes.ToString());
                    if (i+1 < story.StoryInfos.Length)
                    {
                        d += NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.ContentsSeparator, "\n");
                    }
                    data.Append(d);
                    num++;
                }
            }
            data.Append(NewLinePat.Replace(MainWindow.pmanager.Project.ExportSettings.EndChar, "\n"));
            File.WriteAllText(dst, data.ToString());
        }

        private string IndentedString(string src, string key, string v)
        {
            if (MainWindow.pmanager.Project.ExportSettings.IsIndented)
            {
                return Regex.Replace(
                    src,
                    @$"^(?<indent>\s*)(?<before>[^\n]*?)%\((?<value>{key})\)(?<after>[^\n]*)",
                    match =>
                    {
                        var indent = match.Groups["indent"].Value;
                        var r =  indent + match.Groups["before"].Value + Regex.Replace(v, @"(?<=\n)", match => indent) + match.Groups["after"].Value;
                        return r;
                    },
                    RegexOptions.Multiline
                );
            }
            return Regex.Replace(src, @$"(?<!\)%\({key}\)", v);
        }


        private string HandleString(string s)
        {
            if (MainWindow.pmanager.Project.ExportSettings.IsEscape)
            {
                return Regex.Replace(s.Replace(@"\", @"\\"), @"\r?\n", @"\n");
            }
            return s;
        }

        public void ExportProject(string dest, BuildProgressWindow progress, FilePackingData data, Action<Action> dispatcher)
        {
            var baseDir = Path.Combine(dest, title);
            FileSystem.CopyDirectory(_ExportProject(dest, data, progress, dispatcher).BaseDir, baseDir, UIOption.AllDialogs, UICancelOption.DoNothing);
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
            var r = Path.Combine(baseDir, "runtime");
            var runtime_emeral = Path.Combine(r, "emeral.dll");
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
            Directory.CreateDirectory(r);
            if (!File.Exists(runtime_emeral))
            {
                File.Copy("gameruntime/emeral.dll", runtime_emeral);
            }
            var files = new CompilationFiles(target);
            File.WriteAllText(files.SaveDataManager, """
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
            File.WriteAllText(files.Main, GenerateCompilationCode());
            File.WriteAllText(files.MainXaml, $$"""
                <Window x:Class="Game.MainWindow"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                        xmlns:local="clr-namespace:Game"
                        mc:Ignorable="d"
                        Title="{{MainWindow.pmanager.Project.Title}}" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}" Background="Black" ResizeMode="CanMinimize" WindowStartupLocation="CenterScreen" Name="Main">
                        <DockPanel>
                            <DockPanel.LayoutTransform>
                                <ScaleTransform x:Name="Scale"/>
                            </DockPanel.LayoutTransform>
                            <Menu Name="WindowMenu" DockPanel.Dock="Top" IsMainMenu="True" Visibility="Collapsed" DockPanel.ZIndex="1">
                                <MenuItem Header="ファイル">
                                    <MenuItem Name="SaveMenu" Header="セーブ" Click="OpenSaveDialog"/>
                                    <MenuItem Name="LoadMenu" Header="ロード" Click="OpenLoadDialog"/>
                                </MenuItem>
                                <MenuItem Header="ウィンドウ">
                                    <MenuItem Name="MaximizeMenu" Header="最大化" IsCheckable="True" IsChecked="False"/>
                                </MenuItem>
                            </Menu>
                            <Grid DockPanel.ZIndex="0">
                                <Frame Name="Screen" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}" NavigationUIVisibility="Hidden"/>
                                <Rectangle Name="Transition" Height="{Binding ActualHeight, ElementName=Main}" Width="{Binding ActualWidth, ElementName=Main}" Opacity="0" IsHitTestVisible="False"/>
                            </Grid>
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
                            OnPropertyChanged(@"{{p.Value}}");
                        }
                    }
                    """);
            }
            File.WriteAllText(files.TitleXaml, $"""
                <Page x:Class="Game.TitlePage"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    {xaml}
                </Page>
                """);
            File.WriteAllText(files.Title, $$"""
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
            File.WriteAllText(files.GamePageXaml, $"""
                <Page
                    x:Class="Game.GamePage"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    {GenerateGameUIXaml(false)}
                </Page>
                """);
            File.WriteAllText(files.SaveDataXaml, $$"""
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
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot1" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time1"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data2" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot2" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time2"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data3" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot3" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time3"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data4" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot4" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time4"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data5" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot5" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time5"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data6" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot6" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time6"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data7" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot7" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time7"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data8" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot8" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time8"/>
                            </Grid>
                        </StackPanel>
                        <StackPanel Name="Data9" Width="{Binding DataWidth}" Height="{Binding DataHeight}" Background="Black" MouseEnter="OnDataMouseEnter" MouseLeftButtonUp="OnDataMouseLeftUp">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="2*"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}" IsHitTestVisible="False" Name="ScreenShot9" Stretch="Uniform"/>
                                <Label IsHitTestVisible="False" Grid.Row="1" Name="Time9"/>
                            </Grid>
                        </StackPanel>
                    </UniformGrid>
                </Page>
                
                """);
            File.WriteAllText(files.SaveData, $$"""
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
                                MainWindow.PlayAudio(@"{hashTable[MainWindow.pmanager.Project.MouseOverSE]}");
                            """)}}
                        }

                        private void OnDataMouseLeftUp(object sender, MouseButtonEventArgs e)
                        {
                            if (Game.IsLoading) return;
                            {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"""
                                MainWindow.PlayAudio(@"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}");
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
            File.WriteAllText(files.BacklogXaml, $"""
                 <StackPanel x:Class="Game.Backlog"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                      Width="{MainWindow.pmanager.Project.Size[0]}" Height="{MainWindow.pmanager.Project.Size[1]}">
                </StackPanel>
                """);
            File.WriteAllText(files.Backlog, $$"""
                using System.Windows;
                using System.Windows.Input;
                using System.Windows.Controls;
                using System.Windows.Media.Imaging;
                using System.Windows.Media;
                using System.Windows.Media.Animation;
                using System.IO;
                using System.Reflection;
                using System.Text;

                namespace Game
                {
                    public partial class Backlog : StackPanel
                    {
                        private const int PART_HEIGHT = {{MainWindow.pmanager.Project.Size[1] / 5}};
                        public ScrollViewer BacklogViewer;
                        private GamePage game;
                        private Grid BacklogGrid;
                        private bool IsLeftPressed;
                        private double PreY;
                        private double PreDy;

                        public Backlog(GamePage game)
                        {
                            this.game = game;
                            var bg = new SolidColorBrush(Brushes.Black.Color);
                            bg.Opacity = 0.6;
                            Background = bg;
                            BacklogViewer = new ScrollViewer()
                            {
                                Width = {{MainWindow.pmanager.Project.Size[0]}},
                                Height = {{MainWindow.pmanager.Project.Size[1]}},
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                            };
                            BacklogGrid = new Grid();
                            BacklogGrid.MouseLeftButtonDown += (sender, e) => {
                                IsLeftPressed = true;
                                PreY = e.GetPosition(BacklogGrid).Y;
                            };
                            BacklogGrid.MouseLeftButtonDown += (sender, e) => {
                                IsLeftPressed = false;
                            };
                            BacklogGrid.MouseMove += (sender, e) => {
                                var y = e.GetPosition(BacklogGrid).Y;
                                var dy = - y + PreY;
                                if (IsLeftPressed && PreDy != -dy) {
                                    BacklogViewer.ScrollToVerticalOffset(BacklogViewer.VerticalOffset + dy);
                                    PreY = y;
                                    PreDy = dy;
                                }
                            };
                            BacklogViewer.Content = BacklogGrid;
                            Children.Add(BacklogViewer);
                        }

                        public void AddLog(string script, int sceneid, int scriptid)
                        {
                            var grid = new Grid(){
                                Width = {{MainWindow.pmanager.Project.Size[0]}},
                                Height = PART_HEIGHT
                            };
                            grid.ColumnDefinitions.Add(new ColumnDefinition(){
                                Width = new GridLength(3, GridUnitType.Star)
                            });
                            grid.ColumnDefinitions.Add(new ColumnDefinition(){
                                Width = new GridLength(1, GridUnitType.Star)
                            });
                            grid.Children.Add(new TextBlock(){
                                Text = script,
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = {{MainWindow.pmanager.Project.Size[1] / 20}},
                                Foreground = Brushes.White
                            });
                            var btn_grid = new Grid();
                            var jump_btn = new Button(){
                                Content = "ジャンプ"
                            };
                            jump_btn.Click += (sender, e) => {
                                game.GoTo(sceneid, scriptid, () => {
                                    this.Visibility = Visibility.Hidden;
                                });
                            };
                            btn_grid.Children.Add(jump_btn);
                            Grid.SetColumn(btn_grid, 1);
                            grid.Children.Add(btn_grid);
                            BacklogGrid.RowDefinitions.Add(new RowDefinition(){
                                Height = new GridLength(5)
                            });
                            var sep = new Separator();
                            Grid.SetRow(sep, BacklogGrid.RowDefinitions.Count - 1);
                            BacklogGrid.Children.Add(sep);
                            BacklogGrid.RowDefinitions.Add(new RowDefinition(){
                                Height = new GridLength(PART_HEIGHT)
                            });
                            Grid.SetRow(grid, BacklogGrid.RowDefinitions.Count - 1);
                            BacklogGrid.Children.Add(grid);
                        }
                    }
                }
                """);
            var stories = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                stories = GenerateStoryCode();
            });
            File.WriteAllText(files.GamePage, $$"""
                using System.Windows;
                using System.Windows.Input;
                using System.Runtime.InteropServices;
                using System.Windows.Controls;
                using System.Windows.Media.Imaging;
                using System.Windows.Media;
                using System.Windows.Media.Animation;
                using System.IO;
                using System.Reflection;
                using System.Text;

                namespace Game
                {
                    public partial class GamePage : Page
                    {
                        private const int FADEOUT_SAMPLES = 10;
                        private const int FADEOUT_MILLISECOND = 1000 / FADEOUT_SAMPLES;
                        private const float BGM_VOLUME = 0.5f;
                        public int CurrentScriptId, CurrentScene;
                        public string CurrentScript;
                        private MainWindow window;
                        public Backlog backlog;
                        public bool IsLoading;
                        public bool IsHandling;
                        private bool IsNowScripting;
                        private MouseButtonEventHandler OnMouseLeftDown = (sender, e) => {};
                        private RoutedEventHandler MediaEnded = (sender, e) => {};
                        private TempFile _movieFile;

                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern int PlayLoopAudioWithBytes(byte[] b, int len);
                
                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern void StopAllAudio();

                        public GamePage(MainWindow w)
                        {
                            InitializeComponent();
                            window = w;
                            _movieFile = new();
                            backlog = new Backlog(this) {
                                Visibility = Visibility.Hidden
                            };
                            Grid.SetZIndex(backlog, 1);
                            MainGrid.Children.Add(backlog);
                            Unloaded += (sender, e) => {
                                if (_movieFile is not null)
                                {
                                    _movieFile.Dispose();
                                }
                            };
                            CurrentScript = "";
                        }
                
                        private void SetCharacter(Image chara, double x, bool trans)
                        {
                            Canvas.SetLeft(chara, x);
                            if (trans)
                            {
                                var b = new DoubleAnimation() {
                                    From = 0.0,
                                    To = 1.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                                };
                                chara.BeginAnimation(UIElement.OpacityProperty, b);
                            }
                            CharacterPictures.Children.Add(chara);
                        }
                
                        private void RemoveCharas(UIElement[] charas)
                        {
                            foreach (UIElement c in charas)
                            {
                                var b = new DoubleAnimation() {
                                    From = 1.0,
                                    To = 0.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                                };
                                b.Completed += (sender, e) => {
                                    CharacterPictures.Children.Remove(c);
                                };
                                c.BeginAnimation(UIElement.OpacityProperty, b);
                            }
                        }

                        private void RemoveCharas()
                        {
                            foreach (UIElement c in CharacterPictures.Children)
                            {
                                var b = new DoubleAnimation() {
                                    From = 1.0,
                                    To = 0.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                                };
                                b.Completed += (sender, e) => {
                                    CharacterPictures.Children.Remove(c);
                                };
                                c.BeginAnimation(UIElement.OpacityProperty, b);
                            }
                        }
                        
                
                        private void PlayBgm(byte[] b)
                        {
                            PlayLoopAudioWithBytes(b, b.Length);
                        }
                
                        private void FinishBgm()
                        {
                            StopAllAudio();
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
                            CharacterPictures.Children.Clear();
                            Script.Text = "";
                            MessageWindowCanvas.Visibility = Visibility.Hidden;
                        }

                        public void GoTo(int scene, int script, Action func=null)
                        {
                            if (IsLoading) return;
                            IsLoading = true;
                            window.Transition.Fill = Brushes.Black;
                            var fadeout = new DoubleAnimation()
                            {
                                From = 0,
                                To = 1,
                                Duration = new Duration(TimeSpan.FromSeconds(1))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadeout, window.Transition);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadeout);
                            b1.Completed += async (sender, e) => {
                                Clear();
                                window.Screen.Navigate(this);
                                if (func is not null) func();
                                var f = GetType().GetMethod($"Scene{scene}", BindingFlags.Instance | BindingFlags.Public);
                                f.Invoke(this, new object[] { script });
                                IsLoading = false;
                            };
                            b1.Begin();
                        }

                       {{stories}}
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
                }
                """);
            return files;
        }
        private string HandleXaml(string xaml, Dictionary<string, string> d)
        {
            var n = 1;
            return XamlHelper.SourceRegex.Replace(xaml, s =>
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
            var msg = CheckDotNetSDK();
            if (msg != "")
            {
                throw new DotNetException(msg);
            }
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
                    Arguments = $"publish \"{files.Csproj}\" -c Release --self-contained true -r win-x64 -p:ReadyToRun=true -p:AssemblyName=\"{MainWindow.pmanager.ProjectName}\" -o \"{Path.Combine(dest, MainWindow.pmanager.ProjectName)}\"",
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
            return $$"""
                    <DockPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Name="MainPanel" Background="Black" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}">
                        {{(IsScript? """
                        <DockPanel.LayoutTransform>
                            <ScaleTransform x:Name="Scale"/>
                        </DockPanel.LayoutTransform>
                        <Menu Name="WindowMenu" DockPanel.Dock="Top" Visibility="Collapsed">
                            <MenuItem Header="ウィンドウ">
                                <MenuItem Name="MaximizeMenu" Header="最大化" IsCheckable="True" IsChecked="False"/>
                            </MenuItem>
                        </Menu>
                """ : "")}}
                        <Grid Name="MainGrid" Height="{{MainWindow.pmanager.Project.Size[1]}}" Width="{{MainWindow.pmanager.Project.Size[0]}}">
                            <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}"  Name="Bg" Stretch="Uniform"/>
                            <Canvas Name="CharacterPictures"/>
                            <Canvas Name="MessageWindowCanvas">
                                <StackPanel Name="MessageWindow" VerticalAlignment="Bottom">
                                    <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}"  Name="MessageWindowBg" Stretch="Fill"/>
                                </StackPanel>
                                <TextBlock Name="Script" TextAlignment="Left" HorizontalAlignment="Left" VerticalAlignment="Top" TextWrapping="WrapWithOverflow"/>
                                <Canvas Name="NamePlate">
                                    <Image RenderOptions.BitmapScalingMode="{{ScalingMode}}"  Name="NamePlateBgImage" Height="{Binding ActualHeight, ElementName=NamePlate}" Width="{Binding ActualWidth, ElementName=NamePlate}" Stretch="Fill"/>
                                    <Label Name="Speaker" Content="名前" FontSize="30" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Canvas>
                            </Canvas>
                            <MediaElement Name="MoviePlayer" Stretch="Uniform" LoadedBehavior="Manual"/>
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
            var src = GenerateScriptCode(start);
            File.WriteAllText(Path.Combine(MainWindow.pmanager.ActualProjectDir, "script.cs"), src);
            var script = CSharpScript.Create(src, ScriptOptions.Default
                                                                          .WithReferences(references)
                                                                          .AddImports(
                                                                          "System",
                                                                          "System.Linq",
                                                                          "System.Text",
                                                                          "System.Windows",
                                                                          "System.Windows.Data",
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
                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                {{(IsScript ? "End();" : "var b = window.End();\nb.Completed += (sender, e) => {\r\n    IsHandling = false;  \r\n};\r\n        MoviePlayer.Source = null;\r\n                    window.SaveMenu.IsEnabled = true;\r\nwindow.LoadMenu.IsEnabled = true;\r\n        _movieFile.Dispose();")}}
                """;
            var content_counter = 0;
            var scene_counter = 0;
            var script_counter = 0;
            var story_ref = story.StoryInfos.Length;
            var msw = "";
            var trans = IsScript ? "Transition" : "window.Transition";
            var pre_content = new ContentInfo();
            foreach (var t in story.StoryInfos)
            {
                content_counter++;
                var isLastEpisode = content_counter == story_ref;
                var premsw = "";
                var pre_scene = new SceneInfo();
                if (string.IsNullOrEmpty(t.FullPath))
                {
                    stories.AppendLine($$"""
                        public void Content{{content_counter}}() {
                            {{(isLastEpisode ? end_func : $"Content{content_counter + 1}();")}}
                        }
                        """);
                }
                else if (t.IsScenes())
                {
                    var episode = emanager.GetEpisode(t.FullPath);
                    if (!start_scene_flag && !episode.smanager.scenes.Values.Contains(start_scene))
                    {
                        content_counter--;
                        story_ref--;
                        continue;
                    }
                    var scene_ref = episode.smanager.scenes.Count;
                    switch (pre_content.trans)
                    {
                        case TransitionTypes.NONE:
                            stories.AppendLine($$"""
                            public async void Content{{content_counter}}() {
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                MoviePlayer.Source = null;
                                await Task.Delay({{pre_content.interval * 1000}});
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                Scene{{scene_counter + 1}}();
                            }
                            """);
                            break;
                        case TransitionTypes.SIMPLE:
                            stories.AppendLine($$"""
                            public async void Content{{content_counter}}() {
                                IsHandling = true;
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                {{trans}}.Fill = MainWindow.GetBrush(@"{{pre_content.trans_color}}");
                                var fadeout = new DoubleAnimation()
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadeout}}))
                                };
                                var b1 = new Storyboard();
                                Storyboard.SetTarget(fadeout, {{trans}});
                                Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                                b1.Children.Add(fadeout);
                                var fadein = new DoubleAnimation()
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadein}}))
                                };
                                var b2 = new Storyboard();
                                Storyboard.SetTarget(fadein, {{trans}});
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
                            var speaker = string.IsNullOrEmpty(script.speaker)
                                          ? "NamePlate.Visibility = Visibility.Hidden;"
                                          : $"""
                                          Speaker.Content = @"{script.speaker}";
                                          NamePlate.Visibility = Visibility.Visible;
                                          """;
                            var charas = new StringBuilder();
                            charas.AppendLine("var charas = CharacterPictures.Children.Cast<UIElement>().ToArray();");
                            if (0 < script.charas.Count)
                            {
                                var per_x = MainWindow.pmanager.Project.Size[0] / (script.charas.Count * 2);
                                var j = 1;
                                charas.AppendLine($"""
                                        BitmapImage c_bmp;
                                        Image c_img;
                                        var chara_trans = {(script.charas.Count != pre_script?.charas.Count).ToString().ToLower()};
                                        """);
                                foreach (var c in script.charas)
                                {
                                    if (!string.IsNullOrEmpty(c))
                                    {
                                        var file = ConvertPath(Path.Combine("Characters", c));
                                        var bmp = Utils.CreateBmp(MainWindow.pmanager.GetResource("Characters", c));
                                        charas.AppendLine($$"""
                                        c_bmp = MainWindow.CreateBmp(MainWindow.GetResource(@"{{file}}"));
                                        c_img = new Image() {
                                            Source = c_bmp,
                                            Stretch = Stretch.Uniform,
                                            Height = {{MainWindow.pmanager.Project.Size[1]}}
                                        };
                                        RenderOptions.SetBitmapScalingMode(c_img, BitmapScalingMode.{{ScalingMode}});
                                        SetCharacter(c_img, {{per_x * (j * 2 - 1)}} - {{bmp.Width * Math.Min(MainWindow.pmanager.Project.Size[0] / bmp.Width, MainWindow.pmanager.Project.Size[1] / bmp.Height) / 2}}, chara_trans);
                                        """);
                                        j++;
                                    }
                                }
                                charas.AppendLine("if (0 < charas.Length) RemoveCharas(charas);");
                            }
                            var var_script = $"var script = Encoding.UTF8.GetString(Convert.FromBase64String(\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(script.script))}\"));";
                            if (isLastScene && isLastScript)
                            {
                                stories.AppendLine($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            {{var_script}}
                            CurrentScript = script;
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = async (sender, e) => {
                                if (IsHandling {{(IsScript ? "" : "|| window.Screen.Content is not GamePage")}}) return;
                                else if (MessageWindowCanvas.Visibility == Visibility.Visible){
                                    if (IsNowScripting) {
                                        terminate_scripting = true;
                                    }
                                    else{
                                        FinishBgm();
                                        {{(isLastEpisode ? end_func : $"Content{content_counter + 1}();")}}
                                    }
                                }
                                else {
                                    MessageWindowCanvas.Visibility = Visibility.Visible;
                                }
                            };
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            {{(IsScript ? "" :
                            $"""
                            backlog.AddLog(script, {scene_counter}, {script_counter});
                            """)}}
                            foreach (var s in script) {
                                if (terminate_scripting) {
                                    Script.Text = script;
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{MainWindow.pmanager.Project.TextInterval}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            else if (isLastScript)
                            {
                                stories.AppendLine($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            {{var_script}}
                            CurrentScript = script;
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = (sender, e) => {
                                if (IsHandling {{(IsScript ? "" : "|| window.Screen.Content is not GamePage")}}) return;
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
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            {{(IsScript ? "" :
                            $"""
                            backlog.AddLog(script, {scene_counter}, {script_counter});
                            """)}}
                            foreach (var s in script) {
                                if (terminate_scripting) {
                                    Script.Text = script;
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{MainWindow.pmanager.Project.TextInterval}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            else
                            {
                                stories.AppendLine($$"""
                        public async void ShowScript{{script_counter}}() {
                            var terminate_scripting = false;
                            Script.Text = "";
                            {{var_script}}
                            CurrentScript = script;
                            CurrentScriptId = {{script_counter}};
                            {{speaker}}
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown -= OnMouseLeftDown;
                            OnMouseLeftDown = (sender, e) => {
                                if (IsHandling {{(IsScript ? "" : "|| window.Screen.Content is not GamePage")}}) return;
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
                            {{(IsScript ? "" : "window.")}}MouseLeftButtonDown += OnMouseLeftDown;
                            MessageWindowCanvas.Visibility = Visibility.Visible;
                            IsNowScripting = true;
                            {{charas}}
                            {{(IsScript ? "" :
                            $"""
                            backlog.AddLog(script, {scene_counter}, {script_counter});
                            """)}}
                            foreach (var s in script) {
                                if (terminate_scripting) {
                                    Script.Text = script;
                                    terminate_scripting = false;
                                    break;
                                }
                                Script.Text += s.ToString();
                                await Task.Delay({{MainWindow.pmanager.Project.TextInterval}});
                            }
                            IsNowScripting = false;
                        }
                        """);
                            }
                            pre_script = script;
                        }
                        var window = mmanager[s.Value.msw];
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
                        if (string.IsNullOrEmpty(s.Value.bgm))
                        {
                            bgm = $"FinishBgm();";
                        }
                        else
                        {
                            bgm = $$"""PlayBgm(MainWindow.GetResource(@"{{ConvertPath(s.Value.bgm)}}"));""";
                        }
                        var is_bgm_continuing = pre_scene.bgm == s.Value.bgm;
                        var interval = 0 < pre_scene.interval ? $"""
                            await Task.Delay({pre_scene.interval * 1000});
                            """ : "";
                        var msw_bg = ImageUtils.GetFileName(window.MessageWindowBg.Source);
                        var msw_img = msw_bg.Length > 0 ? $"MessageWindowBg.Source = MainWindow.CreateBmp(MainWindow.GetResource(@\"{ConvertPath(msw_bg)}\"))" : "";
                        var plate_img = window.NamePlateBg.Source is null ? "" : $"NamePlateBgImage.Source = MainWindow.CreateBmp(MainWindow.GetResource(@\"{ConvertPath(ImageUtils.GetFileName(window.NamePlateBg.Source))}\"));";
                        msw = $"""
                                    Script.Text = "";
                                    {bg}
                                    {msw_img};
                                    MessageWindow.Width = {window.WindowContents.Width};
                                    MessageWindow.Height = {window.WindowContents.Height};
                                    MessageWindowBg.Width = {window.WindowContents.Width};
                                    MessageWindowBg.Height = {window.WindowContents.Height};
                                    MessageWindow.Background = MainWindow.GetBrush(@"{Utils.GetHex(window.WindowContents.Background)}");
                                    MessageWindow.Background.Opacity = {window.WindowContents.Background.Opacity};
                                    MessageWindowBg.Opacity = {window.MessageWindowBg.Opacity};
                                    Canvas.SetLeft(Script, {Canvas.GetLeft(window.Script)});
                                    Canvas.SetTop(Script, {Canvas.GetTop(window.Script)});
                                    Script.Width = {window.Script.Width};
                                    Script.FontSize = {window.Script.FontSize};
                                    Script.FontFamily = new FontFamily(@"{window.Script.FontFamily}");
                                    Script.Foreground = MainWindow.GetBrush(@"{Utils.GetHex(window.Script.Foreground)}");
                                    Canvas.SetLeft(NamePlate, {Canvas.GetLeft(window.NamePlate)});
                                    Canvas.SetTop(NamePlate, {Canvas.GetTop(window.NamePlate)});
                                    NamePlate.Width = {window.NamePlate.Width};
                                    NamePlate.Height = {window.NamePlate.Height};
                                    NamePlate.Background = MainWindow.GetBrush(@"{Utils.GetHex(window.NamePlate.Background)}");
                                    NamePlate.Background.Opacity = {window.NamePlate.Background.Opacity};
                                    {plate_img}
                                    NamePlateBgImage.Opacity = {window.NamePlateBg.Opacity};
                                    Speaker.Foreground = MainWindow.GetBrush(@"{Utils.GetHex(window.CharaName.Foreground)}");
                                    Speaker.FontFamily = new FontFamily(@"{window.CharaName.FontFamily}");
                                    Speaker.FontSize = {window.CharaName.FontSize};
                                    Canvas.SetLeft(MessageWindow, {Canvas.GetLeft(window.WindowContents)});
                                    Canvas.SetTop(MessageWindow,{Canvas.GetTop(window.WindowContents)});
                                """;
                        if (premsw == s.Value.msw)
                        {
                            switch (pre_scene.trans)
                            {
                                case TransitionTypes.NONE:
                                    stories.AppendLine($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        Script.Text = "";
                        {{(is_bgm_continuing ? "" : "FinishBgm();")}}
                        CurrentScene = {{scene_counter}};
                        {{bg}}
                        if (script == -1)
                        {
                            {{interval}}
                            {{(is_bgm_continuing ? "" : bgm)}}
                            {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                            IsHandling = false;
                            ShowScript{{start_script_num}}();
                        }
                        else
                        {
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds(0.5))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadein);
                            b1.Completed += async (sender, e) => {
                                {{msw}}
                                {{bgm}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                IsHandling = false;
                                var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                                f.Invoke(this, null);
                            };
                            b1.Begin();
                        }
                    }
                    """);
                                    break;
                                case TransitionTypes.SIMPLE:
                                    stories.AppendLine($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                        var bg_loaded = false;
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        {{(is_bgm_continuing ? "" : "FinishBgm();")}}
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        if (script == -1)
                        {
                           {{trans}}.Fill = MainWindow.GetBrush(@"{{pre_scene.trans_color}}");
                            var fadeout = new DoubleAnimation()
                            {
                                From = 0,
                                To = 1,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadeout}}))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadeout, {{trans}});
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
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b2.Children.Add(fadein);
                            b2.Completed += async (sender, e) => {
                                {{(is_bgm_continuing ? "" : bgm)}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
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
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds(0.5))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadein);
                            b1.Completed += async (sender, e) => {
                                {{bg}}
                                CharacterPictures.Children.Clear();
                                while ({{condition}}) {
                                    await Task.Delay(500);
                                }
                                {{msw}}
                                bg_loaded = true;
                                {{bgm}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                IsHandling = false;
                                var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                                f.Invoke(this, null);
                            };
                            b1.Begin();
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
                                    stories.AppendLine($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                        {{(is_bgm_continuing ? "" : "FinishBgm();")}}
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        {{msw}}
                        if (script == -1)
                        {
                            {{interval}}
                            {{(is_bgm_continuing ? "" : bgm)}}
                            {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                            IsHandling = false;
                            ShowScript{{start_script_num}}();
                        }
                        else
                        {
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds(0.5))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadein);
                            b1.Completed += async (sender, e) => {
                                {{bgm}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                IsHandling = false;
                                var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                                f.Invoke(this, null);
                            };
                            b1.Begin();
                        }
                    }
                    """);
                                    break;
                                case TransitionTypes.SIMPLE:
                                    stories.AppendLine($$"""
                    public async void Scene{{scene_counter}}(int script=-1) {
                        if (IsHandling) return;
                        IsHandling = true;
                        {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                        MessageWindowCanvas.Visibility = Visibility.Hidden;
                        {{(is_bgm_continuing ? "" : "FinishBgm();")}}
                        Script.Text = "";
                        CurrentScene = {{scene_counter}};
                        if (script == -1)
                        {
                            {{trans}}.Fill = MainWindow.GetBrush(@"{{pre_scene.trans_color}}");
                            var fadeout = new DoubleAnimation()
                            {
                                From = 0,
                                To = 1,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadeout}}))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadeout, {{trans}});
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadeout);
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds({{pre_scene.fadein}}))
                            };
                            var b2 = new Storyboard();
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b2.Children.Add(fadein);
                            b2.Completed += (sender, e) => {
                                {{(is_bgm_continuing ? "" : bgm)}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
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
                                CharacterPictures.Children.Clear();
                                {{interval}}
                                b2.Begin();
                            };
                            b1.Begin();
                        }
                        else
                        {
                            var fadein = new DoubleAnimation()
                            {
                                From = 1,
                                To = 0,
                                Duration = new Duration(TimeSpan.FromSeconds(0.5))
                            };
                            var b1 = new Storyboard();
                            Storyboard.SetTarget(fadein, {{trans}});
                            Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                            b1.Children.Add(fadein);
                            b1.Completed += async (sender, e) => {
                                {{msw}}
                                {{bgm}}
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = true;\nwindow.LoadMenu.IsEnabled = true;")}}
                                IsHandling = false;
                                var f = GetType().GetMethod($"ShowScript{script}", BindingFlags.Instance | BindingFlags.Public);
                                f.Invoke(this, null);
                            };
                            b1.Begin();
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
                        content_counter--;
                        story_ref--;
                        continue;
                    }
                    switch (pre_content.trans)
                    {
                        case TransitionTypes.NONE:
                            stories.AppendLine($$"""
                            public async void Content{{content_counter}}() {
                                IsHandling = true;
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                                {{(IsScript ? $"MoviePlayer.Source = new Uri(MainWindow.GetResource(@\"{t.GetRelPathToResource()}\"));" : $"    _movieFile = new TempFile(@\"{Path.GetExtension(t.FullPath)}\");\r\n    _movieFile.Write(MainWindow.GetResource(@\"{ConvertPath(t.GetRelPathToResource())}\"));\r\n    MoviePlayer.Source = new Uri(_movieFile.path);")}}
                                if (MediaEnded is not null) {
                                    MoviePlayer.MediaEnded -= MediaEnded;
                                }
                                MediaEnded = (sender, e) => {
                                    IsHandling = false;
                                    {{(isLastEpisode ? end_func : $"Content{content_counter + 1}();")}}
                                };
                                Bg.Source = null;
                                CharacterPictures.Children.Clear();
                                MessageWindowCanvas.Visibility = Visibility.Collapsed;
                                MoviePlayer.MediaEnded += MediaEnded;
                                await Task.Delay({{pre_content.interval * 1000}});
                                MoviePlayer.Play();
                            }
                            """);
                            break;
                        case TransitionTypes.SIMPLE:
                            stories.AppendLine($$"""
                            public async void Content{{content_counter}}() {
                                IsHandling = true;
                                {{(IsScript ? "" : "window.SaveMenu.IsEnabled = false;\nwindow.LoadMenu.IsEnabled = false;")}}
                                MessageWindowCanvas.Visibility = Visibility.Hidden;
                                {{trans}}.Fill = MainWindow.GetBrush(@"{{pre_content.trans_color}}");
                                var fadeout = new DoubleAnimation()
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadeout}}))
                                };
                                var b1 = new Storyboard();
                                Storyboard.SetTarget(fadeout, {{trans}});
                                Storyboard.SetTargetProperty(fadeout, new PropertyPath(UIElement.OpacityProperty));
                                b1.Children.Add(fadeout);
                                var fadein = new DoubleAnimation()
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = new Duration(TimeSpan.FromSeconds({{pre_content.fadein}}))
                                };
                                var b2 = new Storyboard();
                                Storyboard.SetTarget(fadein, {{trans}});
                                Storyboard.SetTargetProperty(fadein, new PropertyPath(UIElement.OpacityProperty));
                                b2.Children.Add(fadein);
                                b2.Completed += async (sender, e) => {
                                    MainPanel.MouseLeftButtonDown -= OnMouseLeftDown;
                                    {{(IsScript ? $"MoviePlayer.Source = new Uri(MainWindow.GetResource(@\"{t.GetRelPathToResource()}\"));" : $"    _movieFile = new TempFile(@\"{Path.GetExtension(t.FullPath)}\");\r\n    _movieFile.Write(MainWindow.GetResource(@\"{ConvertPath(t.GetRelPathToResource())}\"));\r\n    MoviePlayer.Source = new Uri(_movieFile.path);")}}
                                    if (MediaEnded is not null) {
                                        MoviePlayer.MediaEnded -= MediaEnded;
                                    }
                                    MediaEnded = (sender, e) => {
                                        IsHandling = false;
                                        {{(isLastEpisode ? end_func : $"Content{content_counter + 1}();")}}
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
                    private bool IsBgmFinishing;
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

                    private void SetCharacter(Image chara, double x, bool trans)
                    {
                        Canvas.SetLeft(chara, x);
                        if (trans)
                        {
                            var b = new DoubleAnimation() {
                                From = 0.0,
                                To = 1.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(300))
                            };
                            chara.BeginAnimation(UIElement.OpacityProperty, b);
                        }
                        CharacterPictures.Children.Add(chara);
                    }

                    private void RemoveCharas(UIElement[] charas)
                    {
                        foreach (UIElement c in charas)
                        {
                            var b = new DoubleAnimation() {
                                From = 1.0,
                                To = 0.0,
                                Duration = new Duration(TimeSpan.FromMilliseconds(200))
                            };
                            b.Completed += (sender, e) => {
                                CharacterPictures.Children.Remove(c);
                            };
                            c.BeginAnimation(UIElement.OpacityProperty, b);
                        }
                    }      

                    private async void PlayBgm(string bgm)
                    {
                        while (IsBgmFinishing) {
                            await Task.Delay(100);
                        }
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
                        if (IsBgmFinishing) return;
                        IsBgmFinishing = true;
                        var b = new DoubleAnimation() {
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                        };
                        b.Completed += (sender, e) => {
                            BgmPlayer.Stop();
                            BgmPlayer.Source = null;
                            IsBgmFinishing = false;
                        };
                        BgmPlayer.BeginAnimation(MediaElement.VolumeProperty, b);
                    }
                
                    public void Start() {
                        var xaml = (FrameworkElement)XamlReader.Parse({{$"\"\"\"\n{GenerateGameUIXaml()}\n\"\"\""}});
                        var canvas = new Canvas();
                        canvas.Children.Add(xaml);
                        Transition = new System.Windows.Shapes.Rectangle(){
                            Width = {{MainWindow.pmanager.Project.Size[0]}},
                            Height = {{MainWindow.pmanager.Project.Size[1]}},
                            Opacity = 0,
                            IsHitTestVisible = false
                        };
                        Transition.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth"){
                            Source = this,
                            Mode = BindingMode.OneWay
                        });
                        Transition.SetBinding(FrameworkElement.HeightProperty, new Binding("ActualHeight"){
                            Source = this,
                            Mode = BindingMode.OneWay
                        });
                        canvas.Children.Add(Transition);
                        Content = canvas;
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
                        MouseRightButtonDown += (sender, e) => {
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
                        Content1();
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
            return $$"""
                using Game.Core;
                using System.Runtime.InteropServices;
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
                        private TitlePage TitlePage;
                        private GamePage Game;
                        private SaveDataPage SaveDataPage;
                        private bool fin;
                
                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern IntPtr GetResourceData(string h, out int len);
                        
                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern void Free(IntPtr ptr);

                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern void PlayAudio(string h);

                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern void StopAudio(int n);

                        [DllImport("runtime/emeral.dll", CallingConvention = CallingConvention.Cdecl)]
                        public static extern void StopAllAudio();
                        
                        public static byte[] GetResource(string h)
                        {
                            int len;
                            var ptr = GetResourceData(h, out len);
                            if (ptr != IntPtr.Zero && 0 < len)
                            {
                                byte[] data = new byte[len];
                                Marshal.Copy(ptr, data, 0, len);
                                Free(ptr);
                                return data;
                            }
                            else
                            {
                                Application.Current.Shutdown();
                                return null;
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
                    
                        public MainWindow()
                        {
                            Title = @"{{MainWindow.pmanager.Project.Title}}";
                            Width = {{MainWindow.pmanager.Project.Size[0]}};
                            Height = {{MainWindow.pmanager.Project.Size[1]}};
                            ResizeMode = ResizeMode.CanMinimize;
                            Background = Brushes.Black;
                            TitlePage = new(this);
                            Game = new(this);
                            MouseRightButtonDown += (sender, e) => {
                                if (Screen.Content is GamePage && !Game.IsHandling) {
                                    if (Game.backlog.Visibility == Visibility.Visible) {
                                        Game.backlog.Visibility = Visibility.Hidden;
                                    }
                                    else {
                                        Game.MessageWindowCanvas.Visibility = Game.MessageWindowCanvas.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                                    }
                                }
                            };
                            MouseWheel += (sender, e) => {
                                if (Screen.Content is GamePage && 0 < e.Delta) {
                                    if (!Game.IsHandling && Game.backlog.Visibility == Visibility.Hidden) {
                                        Game.backlog.BacklogViewer.ScrollToEnd();
                                        Game.backlog.Visibility = Visibility.Visible;
                                    }
                                }
                            };
                            Closing += (sender, e) => {
                                var res = MessageBox.Show("終了しますか？", @"{{MainWindow.pmanager.ProjectName}}", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                e.Cancel = res is not MessageBoxResult.Yes;
                                if (!e.Cancel)
                                {
                                    fin = true;
                                    Game.Clear();
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
                                SaveDataPage = new(Game, Screen);
                                ShowTitlePage();
                            };
                            if (TitlePage.FindName("StartButton") is Button sbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                sbtn.MouseEnter += (sender, e) => {
                                    PlayAudio(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
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
                                    Game.Content1();
                                };
                                b1.Completed += (sender, e) => {
                                    Screen.Navigate(Game);
                                    Screen.BeginAnimation(UIElement.OpacityProperty, b2);
                                };
                                sbtn.IsEnabled = false;
                                TitlePage.Loaded += (sender, e) => {
                                    sbtn.IsEnabled = true;
                                };
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlayAudio(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                Screen.BeginAnimation(UIElement.OpacityProperty, b1);
                                };
                            }
                            if (TitlePage.FindName("LoadButton") is Button lbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                lbtn.MouseEnter += (sender, e) => {
                                    PlayAudio(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
                                };
                                """)}}
                                lbtn.Click += (sender, e) => {
                                    {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlayAudio(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                    OpenLoadDialog();
                                };
                            }
                            if (TitlePage.FindName("FinishButton") is Button fbtn)
                            {
                                {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseOverSE) ? "" : $$"""
                                fbtn.MouseEnter += (sender, e) => {
                                    PlayAudio(@"{{hashTable[MainWindow.pmanager.Project.MouseOverSE]}}");
                                };
                                """)}}
                                fbtn.Click += (sender, e) => {
                                    {{(string.IsNullOrEmpty(MainWindow.pmanager.Project.MouseDownSE) ? "" : $"MainWindow.PlayAudio(@\"{hashTable[MainWindow.pmanager.Project.MouseDownSE]}\");")}}
                                    Close();
                                };
                            }
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
                                Game.Clear();
                                Screen.BeginAnimation(UIElement.OpacityProperty, b2);
                            };
                            Game.MessageWindowCanvas.Visibility = Visibility.Hidden;
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

    public class DotNetException : Exception
    {
        public DotNetException() : base()
        {

        }

        public DotNetException(string msg) : base(msg)
        {

        }

        public DotNetException(string msg, Exception inner) : base(msg, inner)
        {

        }
    }

    class FilePackingData
    {
        public int FinishedCount, Length;
        public string FileName;
    }
    public class CompilationFiles
    {
        public string BaseDir, Main, MainXaml, App, AppXaml, AssemblyInfo, Csproj, GamePage, GamePageXaml, Title, TitleXaml, SaveData, SaveDataXaml, SaveDataManager, Backlog, BacklogXaml;

        public CompilationFiles(string dir)
        {
            BaseDir = dir;
            Main = Path.Combine(BaseDir, "MainWindow.xaml.cs");
            MainXaml = Path.Combine(BaseDir, "MainWindow.xaml");
            App = Path.Combine(BaseDir, "App.xaml.cs");
            AppXaml = Path.Combine(BaseDir, "App.xaml");
            AssemblyInfo = Path.Combine(BaseDir, "AssemblyInfo.cs");
            Csproj = Path.Combine(BaseDir, $"Game.csproj");
            GamePage = Path.Combine(BaseDir, "GamePage.xaml.cs");
            GamePageXaml = Path.Combine(BaseDir, "GamePage.xaml");
            Title = Path.Combine(BaseDir, "Title.xaml.cs");
            TitleXaml = Path.Combine(BaseDir, "Title.xaml");
            SaveData = Path.Combine(BaseDir, "SaveDataPage.xaml.cs");
            SaveDataXaml = Path.Combine(BaseDir, "SaveDataPage.xaml");
            SaveDataManager = Path.Combine(BaseDir, "SaveDataManager.cs");
            Backlog = Path.Combine(BaseDir, "Backlog.xaml.cs");
            BacklogXaml = Path.Combine(BaseDir, "Backlog.xaml");
        }
    }
}
