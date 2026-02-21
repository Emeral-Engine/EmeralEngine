using EmeralEngine.Core;
using EmeralEngine.Scene;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace EmeralEngine.Story
{
    public class EpisodeManager
    {
        public Dictionary<string, EpisodeInfo> episodes;
        private string baseDir;
        public EpisodeManager()
        {
            baseDir = MainWindow.pmanager.ProjectEpisodesDir;
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            episodes = new();
            Load();
        }

        public void Load()
        {
            episodes.Clear();
            foreach (var e in GetEpisodes())
            {
                episodes.Add(e.Name, e);
            }
        }

        public EpisodeInfo? GetEpisode(string name)
        {
            return new EpisodeInfo(Path.Combine(baseDir, name));
        }
        public List<EpisodeInfo> GetEpisodes()
        {
            return Directory.GetDirectories(baseDir)
                            .Select(e => new EpisodeInfo(e))
                            .ToList();
        }
        public EpisodeInfo New(string name = "")
        {
            string path;
            if (name == "")
            {
                path = Utils.GetUnusedDirName(Path.Combine(baseDir, "Episode"));
            }
            else
            {
                path = Path.Combine(baseDir, name);
            }
            Directory.CreateDirectory(path);
            var info = new EpisodeInfo(path);
            episodes[Path.GetFileName(path)] = info;
            return info;
        }

        public void Rename(string old, string new_)
        {
            Directory.Move(Path.Combine(baseDir, old), Path.Combine(baseDir, new_));
        }

        public void Dump()
        {
            foreach (var e in episodes.Values)
            {
                e.smanager.Dump();
            }
        }
        public void Dump(string dest)
        {
            foreach (var e in episodes.Values)
            {
                e.smanager.Dump(dest);
            }
        }
    }

    public class EpisodeInfo : BaseInfo
    {
        public SceneManager smanager;
        public string Name
        {
            get => System.IO.Path.GetFileName(Path)
                       .Replace(" ", "_")
                       .Replace("　", "_")
                       .Replace("\t", "_");
        }
        public EpisodeInfo(string path)
        {
            Path = path;
            smanager = new SceneManager(path);
        }
        public ImageSource? GetThumbnail()
        {
            return smanager.scenes.First().Value.thumbnail;
        }
        public void Rename(string name)
        {
            var dest = System.IO.Path.Combine(Path, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), name));
            File.Move(Path, dest);
            Path = dest;
        }
    }
}
