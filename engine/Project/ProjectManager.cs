using EmeralEngine.Builder;
using EmeralEngine.Core;
using EmeralEngine.Story;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace EmeralEngine.Project
{
    public class ProjectManager
    {
        public static string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmeralEngine");
        public static string ProjectsDir = Path.Combine(BaseDir, "Projects");
        public static string ConfigFile = Path.Combine(BaseDir, "config.json");
        public static string RecentProjFile = Path.Combine(BaseDir, "recent.txt");
        public Config config;
        public ProjectConfig Project;
        public string ActualProjectDir;
        public string ProjectFile;
        public string ProjectTitleScreen, ActualProjectTitleScreen;
        public string ProjectEpisodesDir, ActualProjectEpisodesDir;
        public string ProjectResourceDir, ActualProjectResourceDir;
        public string ProjectMswDir, ActualProjectMswDir;
        public string ProjectName;
        public string ProjectDotNet;
        public TempDirectory Temp;
        public ProjectManager()
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            if (!Directory.Exists(ProjectsDir)) Directory.CreateDirectory(ProjectsDir);
            if (File.Exists(ConfigFile))
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile));
            }
            else
            {
                config = new();
            }
            Temp = new();
        }

        public void SetProjectDir(string dir)
        {
            ActualProjectDir = dir;
            UpdatePath();
            AddRecentProject();
        }


        public static ProjectConfig Parse(string path)
        {
            return JsonSerializer.Deserialize<ProjectConfig>(File.ReadAllText(path));
        }


        public string[] GetRecentProjects()
        {
            if (!File.Exists(RecentProjFile)) return new string[]{};
            return File.ReadAllText(RecentProjFile).Split("\n").Distinct().ToArray();
        }

        public void SetRecentProjects(string[] recents)
        {
            File.WriteAllText(RecentProjFile, string.Join("\n", recents.Prepend(ProjectFile)));
        }


        public void AddRecentProject()
        {
            var recents = GetRecentProjects();
            SetRecentProjects(recents);
        }


        public void SaveProject()
        {
            SaveProjectFile();
            if (Directory.Exists(ActualProjectResourceDir))
            {
                Directory.Delete(ActualProjectResourceDir, true);
            }
            if (Directory.Exists(ActualProjectEpisodesDir))
            {
                Directory.Delete(ActualProjectEpisodesDir, true);
            }
            if (Directory.Exists(ActualProjectMswDir))
            {
                Directory.Delete(ActualProjectMswDir, true);
            }
            Directory.CreateDirectory(ActualProjectEpisodesDir);
            Directory.CreateDirectory(ActualProjectMswDir);
            FileSystem.CopyDirectory(ProjectEpisodesDir, ActualProjectEpisodesDir, true);
            FileSystem.CopyDirectory(ProjectMswDir, ActualProjectMswDir, true);
            if (File.Exists(ProjectTitleScreen))
            {
                File.Copy(ProjectTitleScreen, ActualProjectTitleScreen, true);
            }
            FileSystem.CopyDirectory(ProjectResourceDir, ActualProjectResourceDir);
        }

        public void SaveProject(string dest)
        {
            Debug.WriteLine(dest);
            SaveProjectFile(dest);
            var r = Path.Combine(dest, "Resources");
            var e = Path.Combine(dest, "Episodes");
            var m = Path.Combine(dest, "MessageWindows");
            var t = Path.Combine(dest, "titlescreen.xaml");
            if (Directory.Exists(r))
            {
                Directory.Delete(r, true);
            }
            if (Directory.Exists(e))
            {
                Directory.Delete(e, true);
            }
            if (Directory.Exists(m))
            {
                Directory.Delete(m, true);
            }
            Directory.CreateDirectory(e);
            Directory.CreateDirectory(m);
            FileSystem.CopyDirectory(ProjectEpisodesDir, e, true);
            FileSystem.CopyDirectory(ProjectMswDir, m, true);
            if (File.Exists(ProjectTitleScreen))
            {
                File.Copy(ProjectTitleScreen, t, true);
            }
            FileSystem.CopyDirectory(ProjectResourceDir, r);
        }

        public void SaveProjectFile()
        {
            File.WriteAllText(ProjectFile, JsonSerializer.Serialize(Project));
        }

        public void SaveProjectFile(string dest)
        {
            File.WriteAllText(Path.Combine(dest, "project.emeral"), JsonSerializer.Serialize(Project));
        }

        private void UpdatePath()
        {
            ProjectFile = Path.Combine(ActualProjectDir, "project.emeral");
            ProjectTitleScreen = Path.Combine(Temp.path, "titlescreen.xaml");
            ActualProjectTitleScreen = Path.Combine(ActualProjectDir, "titlescreen.xaml");
            ProjectMswDir = Path.Combine(Temp.path, "MessageWindows");
            ActualProjectMswDir = Path.Combine(ActualProjectDir, "MessageWindows");
            ActualProjectEpisodesDir = Path.Combine(ActualProjectDir, "Episodes");
            ActualProjectResourceDir = Path.Combine(ActualProjectDir, "Resources");
            Directory.CreateDirectory(ActualProjectDir);
            Directory.CreateDirectory(ProjectResourceDir);
            Directory.CreateDirectory(ProjectEpisodesDir);
            Directory.CreateDirectory(ProjectMswDir);
            FileSystem.CopyDirectory(ActualProjectDir, Temp.path, true);
        }

        private void Setup(string name)
        {
            if (Temp is not null)
            {
                Temp.Dispose();
            }
            ProjectName = name;
            Temp = new();
            ProjectResourceDir = Path.Combine(Temp.path, "Resources");
            ProjectEpisodesDir = Path.Combine(Temp.path, "Episodes");
            ProjectDotNet = Path.Combine(Temp.path, "dotnet");
            try
            {
                Directory.Delete(ProjectDotNet, true);
            }
            catch { }
            UpdatePath();
        }
        public void LoadProject(string path)
        {
            ProjectFile = path;
            Project = Parse(ProjectFile);
            ActualProjectDir = Directory.GetParent(path).FullName;
            Setup(Project.Title);
            var default_msw = Path.Combine(ProjectMswDir, "0.xaml");
            if (!File.Exists(default_msw))
            {
                File.WriteAllText(default_msw, GetDefaultMsw());
            }
            AddRecentProject();
        }
        public void NewProject(string name, int[] size)
        {
            ActualProjectDir = Path.Combine(ProjectsDir, name);
            Setup(name);
            Project = new()
            {
                Title = name,
                Size = size,
                Story = new(),
                Flags = new List<string>(),
            };
            InitDotnet();
            SaveProject();
        }
        
        public static void InitDotnet(string? target=null)
        {
            var dest = target ?? Path.Combine(MainWindow.pmanager.ActualProjectDir, GameBuilder.DOTNET_DIR);
            var files = new CompilationFiles(dest);
            Directory.CreateDirectory(dest);
            File.WriteAllText(files.AppXaml, """
                <Application x:Class="Game.App"
                             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:local="clr-namespace:Game"
                             StartupUri="MainWindow.xaml">
                    <Application.Resources>

                    </Application.Resources>
                </Application>
                """);
            File.WriteAllText(files.App, """
                using System.Configuration;
                using System.Data;
                using System.Windows;

                namespace Game;

                /// <summary>
                /// Interaction logic for App.xaml
                /// </summary>
                public partial class App : Application
                {
                }
                """);
            File.WriteAllText(files.AssemblyInfo, """
                using System.Windows;

                [assembly:ThemeInfo(
                    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                                //(used if a resource is not found in the page,
                                                                // or application resource dictionaries)
                    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                                //(used if a resource is not found in the page,
                                                                // app, or any theme specific resource dictionaries)
                )]
                """);
            File.WriteAllText(files.Csproj, """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>WinExe</OutputType>
                    <TargetFramework>net10.0-windows</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(files.MainXaml, """
                <Window x:Class="Game.MainWindow"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                        xmlns:local="clr-namespace:Game"
                        mc:Ignorable="d"
                        Title="MainWindow" Height="450" Width="800">
                    <Grid>

                    </Grid>
                </Window>
                """);
            File.WriteAllText(files.Main, """
                using System.Text;
                using System.Windows;
                using System.Windows.Controls;
                using System.Windows.Data;
                using System.Windows.Documents;
                using System.Windows.Input;
                using System.Windows.Media;
                using System.Windows.Media.Imaging;
                using System.Windows.Navigation;
                using System.Windows.Shapes;

                namespace Game;

                /// <summary>
                /// Interaction logic for MainWindow.xaml
                /// </summary>
                public partial class MainWindow : Window
                {
                    public MainWindow()
                    {
                        InitializeComponent();
                    }
                }
                """);
        }
        public string[] GetProjectNames()
        {
            return Directory.GetDirectories(ProjectsDir)
                            .OrderByDescending(Directory.GetLastWriteTime)
                            .Select(p => Path.GetFileName(p))
                            .ToArray();
        }
        public string RelToResourcePath(string name)
        {
            return Path.GetRelativePath(ProjectResourceDir, Path.GetFullPath(Path.Combine(ProjectResourceDir, name)));
        }
        public string[] GetResources()
        {
            return Directory.GetFiles(ProjectResourceDir, "*", System.IO.SearchOption.TopDirectoryOnly);
        }

        public string[] GetAllResources()
        {
            return Directory.GetFiles(ProjectResourceDir, "*", System.IO.SearchOption.AllDirectories);
        }
        public string GetResource(params string[] names)
        {
            var res = ProjectResourceDir;
            foreach (var n in names)
            {
                res = Path.Combine(res, n);
            }
            return res;
        }

        public void AddFlag(string name)
        {
            Project.Flags.Append(name);
        }
        public void RemoveFlag(string name)
        {
            Project.Flags.Remove(name);
        }
        public bool IsExistsFlag(string name)
        {
            return Project.Flags.Contains(name);
        }
        public void ApplyStory(ContentInfo[] s)
        {
            Project.Story = s.ToList();
        }

        public string ReadTitleScreenXaml(string dir)
        {
            if (File.Exists(ProjectTitleScreen))
            {
                return XamlHelper.SourceRegex.Replace(File.ReadAllText(ProjectTitleScreen), s =>
                {
                    return $" Source=\"{Path.Combine(dir, s.Groups[1].Value)}\"";
                });
            }
            else
            {
                var xaml = $$"""
                    <Canvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Name="TitleScreen" Background="Transparent">
                        <Image Name="BackgroundImage" Canvas.ZIndex="0" Width="{Binding Width, ElementName=TitleScreen}" Height="{Binding Height, ElementName=TitleScreen}" Stretch="Fill"/>
                        {{GetNormalButtons()}}
                    </Canvas>
                    """;
                File.WriteAllText(ProjectTitleScreen, xaml);
                return xaml;
                }
        }

        public string GetNormalButtons()
        {
            return $"""
                    <Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Name="ButtonsBorder" Canvas.ZIndex="1" Width="300" Height="250" Canvas.Left="{Project.Size[0] / 2}" Canvas.Top="{Project.Size[1] / 2}" BorderBrush="White" BorderThickness="3">
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

        public string[] GetMessageWindows()
        {
            return Directory.GetFiles(ProjectMswDir, "*.xaml", System.IO.SearchOption.TopDirectoryOnly).Order().ToArray();
        }

        public string GetNextMswPath()
        {
            return Path.Combine(ProjectMswDir, $"{GetMessageWindows().Length}.xaml");
        }

        public string GetDefaultMsw()
        {
            var window_h = Project.Size[1] * 0.3;
            var plate_h = Project.Size[1] * 0.1;
            return $$"""
                 <Canvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <Canvas Name="WindowContents" Width="{{Project.Size[0]}}" Height="{{window_h}}" Canvas.Top="{{Project.Size[1] - window_h}}" Canvas.Left="0">
                        <Canvas.Background>
                            <SolidColorBrush Color="DarkGray" Opacity="0.7"/>
                        </Canvas.Background>
                        <Image Name="MessageWindowBgImage" Stretch="Fill" Height="{Binding ActualHeight, ElementName=WindowContents}" Width="{Binding ActualWidth, ElementName=WindowContents}"/>
                    </Canvas>
                    <TextBlock Name="Script" Width="{{Project.Size[0] * 0.9}}" TextAlignment="Left" HorizontalAlignment="Left" VerticalAlignment="Top"  Canvas.Left="0" Canvas.Top="{{Project.Size[1] - window_h}}" FontSize="30" Foreground="White" TextWrapping="WrapWithOverflow"/>
                    <Canvas Name="NamePlate" Width="{{Project.Size[0] * 0.2}}" Height="{{plate_h}}" Canvas.Left="0" Canvas.Top="{{Project.Size[1] - window_h - plate_h}}">
                        <Canvas.Background>
                            <SolidColorBrush Color="DarkGray" Opacity="0"/>
                        </Canvas.Background>
                        <Image Name="NamePlateBgImage" Stretch="Fill" Height="{Binding ActualHeight, ElementName=NamePlate}" Width="{Binding ActualWidth, ElementName=NamePlate}"/>
                        <Label Name="CharaName" Content="名前" FontSize="30" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Canvas>
                </Canvas>
                """;
        }
    }

    public class BackupManager
    {
        public string buildDir;
        private string baseDir;
        private const int MAX_BACKUPS = 2;
        private int now_backups;
        private string oldest_backup;
        private Managers Managers;
        public BackupManager(Managers m)
        {
            Managers = m;
            baseDir = Path.Combine(Managers.ProjectManager.ActualProjectDir, "Backups");
            buildDir = Path.Combine(baseDir, "LastBuild");
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                Backup();
            }
            var backups = Directory.GetDirectories(baseDir);
            now_backups = backups.Length;
            oldest_backup = backups.OrderBy(f => Directory.GetLastWriteTime(f)).First();
        }

        public void Backup()
        {
            string dest;
            if (MAX_BACKUPS <= now_backups)
            {
                dest = oldest_backup;
                try
                {
                    Directory.Delete(dest, true);
                }
                catch { }
            }
            else
            {
                now_backups++;
                dest = Path.Combine(baseDir, now_backups.ToString());
            }
            Directory.CreateDirectory(dest);
            try
            {
                Managers.EpisodeManager.Dump();
                Managers.ProjectManager.ApplyStory(Managers.StoryManager.StoryInfos);
                Managers.ProjectManager.SaveProject(dest);
            }
            catch { }
        }
        public void BackupForBuild()
        {
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }
            Managers.EpisodeManager.Dump();
            Managers.ProjectManager.ApplyStory(Managers.StoryManager.StoryInfos);
            Managers.ProjectManager.SaveProject(buildDir);
        }
    }

    public class Config
    {
        public void Save()
        {
            File.WriteAllText(ProjectManager.ConfigFile, JsonSerializer.Serialize(this));
        }
    }

    public class ProjectConfig
    {
        public string Title { set; get; } = "仮題";
        public string MouseOverSE { set; get; } = "";
        public string MouseDownSE { set; get; } = "";
        public int[] Size { get; set; } = new int[2];
        public int TextInterval { get; set; } = 60; // ms
        public int ScalingMode { get; set; } = 0; // 0: Linear, 1: Nearest
        public ProjectStartupWindows Startup {  set; get; } = new();
        public EditorSettings EditorSettings { set; get; } = new();
        public SceneSettings SceneSettings { set; get; } = new();
        public CharacterSettings CharacterSettings { set; get; } = new();
        public ExportSettings ExportSettings { set; get; } = new();
        public List<ContentInfo> Story { set; get; } = new();
        public List<string> Flags { set; get; } = new();
    }
    public class ProjectStartupWindows
    {
        public bool Story { set; get; } = false;
        public bool Scene { set; get; } = true;
        public bool Script { set; get; } = true;
        public bool Resource { set; get; } = false;
        public bool Chara { set; get; } = false;
        public bool Msw { set; get; } = false;
    }
    public class  EditorSettings
    {
        public bool AddScriptWhenEmpty { get; set; } = false;
    }

    public class SceneSettings
    {
        public bool ChangeLatterBgWhenChanged { get; set; } = true;
    }

    public class CharacterSettings
    {
        public bool Triming { set; get; } = true;
    }

    public class ExportSettings
    {
        public bool IsEscape { set; get; } = true;
        public bool IsIndented { set; get; } = true;
        public string ContentsSeparator { set; get; } = ",\\n";
        public string ScenesSeparator { set; get; } = ",\\n";
        public string ScriptsSeparator { set; get; } = ",\\n";
        public string PicturesSeparator { set; get; } = ",\\n";
        public string BeginChar { get; set; } = @"{\n";
        public string EndChar { get; set; } = @"\n}";
        public string _ContentFormat = """
                "%(n1)": [
                    %(scenes)
                ]
            """;
        public string ContentFormat
        {
            get => _ContentFormat;
            set
            {
                _ContentFormat = value;
            }
        }
        public string _SceneFormat = """
            {
                "bg": "%(bg)",
                "bgm": "%(bgm)",
                "fadein": %(fadein),
                "fadeout": %(fadeout),
                "wait": %(wait),
                "scripts": [
                    %(scripts)
                ]
            }
            """;
        public string SceneFormat
        {
            get => _SceneFormat;
            set
            {
                _SceneFormat = value;
            }
        }
        public string _ScriptFormat = """
            {
                "pictures": [
                    %(pictures)
                ],
                "speaker": "%(speaker)",
                "script": "%(script)"
            }
            """;
        public string ScriptFormat
        {
            get => _ScriptFormat;
            set
            {
                _ScriptFormat = value;
            }
        }

        public string _PictureFormat = """
            "%(picture)"
            """;
        public string PictureFormat
        {
            get => _PictureFormat;
            set
            {
                _PictureFormat = value;
            }
        }
    }
}