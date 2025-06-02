using EmeralEngine.Story;
using EmeralEngine.MessageWindow;
using EmeralEngine.Scene;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using EmeralEngine.Builder;
using Microsoft.VisualBasic.FileIO;
using EmeralEngine.Core;

namespace EmeralEngine.Project
{
    public class ProjectManager
    {
        public static string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmeralEngine");
        public static string ProjectsDir = Path.Combine(BaseDir, "Projects");
        public static string ConfigFile = Path.Combine(BaseDir, "config.json");
        public Config config;
        public ProjectConfig Project;
        public string ActualProjectDir;
        public string ProjectFile;
        public string ProjectTitleScreen, ActualProjectTitleScreen;
        public string ProjectEpisodesDir, ActualProjectEpisodesDir;
        public string ProjectResourceDir, ActualProjectResourceDir;
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
        }
        public void SaveProject()
        {
            SaveProjectFile();
            var r = Path.Combine(ActualProjectDir, "Resources");
            if (Directory.Exists(r))
            {
                Directory.Delete(r, true);
            }
            FileSystem.CopyDirectory(ProjectResourceDir, r);
        }

        public void SaveProjectFile()
        {
            File.WriteAllText(ProjectFile, JsonSerializer.Serialize(Project));
        }
        private void Setup(string name)
        {
            if (Temp is not null)
            {
                Temp.Dispose();
            }
            Temp = new();
            ProjectName = name;
            ActualProjectDir = Path.Combine(ProjectsDir, ProjectName);
            ProjectResourceDir = Path.Combine(Temp.path, "Resources");
            ActualProjectResourceDir = Path.Combine(ActualProjectDir, "Resources");
            ProjectEpisodesDir = Path.Combine(Temp.path, "Episodes");
            ActualProjectEpisodesDir = Path.Combine(ActualProjectDir, "Episodes");
            ProjectDotNet = Path.Combine(Temp.path, "dotnet");
            ProjectFile = Path.Combine(ActualProjectDir, "project.emeral");
            ProjectTitleScreen = Path.Combine(Temp.path, "titlescreen.xaml");
            ActualProjectTitleScreen = Path.Combine(ActualProjectDir, "titlescreen.xaml");
            if (!Directory.Exists(ActualProjectDir)) Directory.CreateDirectory(ActualProjectDir);
            if (!Directory.Exists(ActualProjectResourceDir)) Directory.CreateDirectory(ProjectResourceDir);
            if (!Directory.Exists(ActualProjectEpisodesDir)) Directory.CreateDirectory(ProjectEpisodesDir);
            Directory.CreateDirectory(ProjectResourceDir);
            Directory.CreateDirectory(ProjectEpisodesDir);
            FileSystem.CopyDirectory(ActualProjectDir, Temp.path, true);
            Directory.SetCurrentDirectory(ProjectResourceDir);
        }
        public void LoadProject(string name)
        {
            Setup(name);
            Project = JsonSerializer.Deserialize<ProjectConfig>(File.ReadAllText(ProjectFile));
        }
        public void NewProject(string name, int[] size)
        {
            Setup(name);
            Project = new()
            {
                Title = name,
                Size = size,
                Startup = new ProjectStartupWindows()
                {
                    Scene = true,
                    Script = true
                },
                EditorSettings = new EditorSettings()
                {
                    AddScriptWhenEmpty = false,
                },
                SceneSettings = new SceneSettings()
                {
                    ChangeLatterBgWhenChanged = true,
                },
                Story = new(),
                Flags = new List<string>()
            };
            InitDotnet();
            SaveProject();
        }
        
        public static void InitDotnet(string? target=null)
        {
            var dest = target ?? Path.Combine(MainWindow.pmanager.ActualProjectDir, GameBuilder.DOTNET_DIR);
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"new wpf -o \"{dest}\" -n \"Game\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            p.Start();
            p.WaitForExit();
            if (!Utils.RaiseError(p))
            {
                return;
            }
            var p2 = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"add \"{dest}\" package ZstdNet",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            p2.Start();
            p2.WaitForExit();
            if (!Utils.RaiseError(p2))
            {
                return;
            }
            var p3 = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"add \"{dest}\" package NAudio",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            p3.Start();
            p3.WaitForExit();
            Utils.RaiseError(p3);
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
                return GameBuilder.SourceRegex.Replace(File.ReadAllText(ProjectTitleScreen), s =>
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
    }

    public class BackupManager
    {
        public string buildDir;
        private string baseDir;
        private const int MAX_BACKUPS = 10;
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
                Directory.Delete(dest, true);
            }
            else
            {
                now_backups++;
                dest = Path.Combine(baseDir, now_backups.ToString());
            }
            Managers.EpisodeManager.Dump();
            var edir = Path.Combine(dest, "Episodes");
            Directory.CreateDirectory(edir);
            FileSystem.CopyDirectory(Managers.ProjectManager.ProjectEpisodesDir, edir);
            if (File.Exists(Managers.ProjectManager.ProjectTitleScreen))
            {
                File.Copy(Managers.ProjectManager.ProjectTitleScreen, Path.Combine(dest, "titlescreen.xaml"), true);
            }
            Managers.ProjectManager.ApplyStory(Managers.StoryManager.StoryInfos);
            Managers.ProjectManager.SaveProject();
            Managers.MessageWindowManager.Dump(Path.Combine(Managers.ProjectManager.ActualProjectDir, MessageWindowManager.FILENAME));
        }
        public void BackupForBuild()
        {
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }
            Managers.EpisodeManager.Dump();
            var edir = Path.Combine(buildDir, "Episodes");
            if (Directory.Exists(edir))
            {
                Directory.Delete(edir, true);
            }
            Directory.CreateDirectory(edir);
            FileSystem.CopyDirectory(Managers.ProjectManager.ProjectEpisodesDir, edir, true);
            if (File.Exists(Managers.ProjectManager.ProjectTitleScreen))
            {
                File.Copy(Managers.ProjectManager.ProjectTitleScreen, Path.Combine(buildDir, "titlescreen.xaml"), true);
            }
            Managers.ProjectManager.ApplyStory(Managers.StoryManager.StoryInfos);
            Managers.ProjectManager.SaveProject();
            Managers.MessageWindowManager.Dump(Path.Combine(Managers.ProjectManager.ActualProjectDir, MessageWindowManager.FILENAME));
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
        public string Title { set; get; }
        public string MouseOverSE {  set; get; }
        public string MouseDownSE {  set; get; }
        public int[] Size { get; set; }
        public ProjectStartupWindows Startup {  set; get; }
        public EditorSettings EditorSettings { set; get; }
        public SceneSettings SceneSettings { set; get; }
        public List<ContentInfo> Story { set; get; }
        public List<string> Flags { set; get; }
    }
    public class ProjectStartupWindows
    {
        public bool Story {  set; get; }
        public bool Scene { set; get; }
        public bool Script {  set; get; }
        public bool Resource {  set; get; }
        public bool Chara {  set; get; }
        public bool Msw {  set; get; }
    }
    public class  EditorSettings
    {
        public bool AddScriptWhenEmpty { get; set; }
    }

    public class SceneSettings
    {
        public bool ChangeLatterBgWhenChanged {  get; set; }
    }
}