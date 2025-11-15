using EmeralEngine.Core;
using EmeralEngine.Scene;
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
            foreach (var e in GetEpisodes())
            {
                episodes.Add(e.name, e);
            }
        }

        public EpisodeInfo GetEpisode(string name)
        {
            return episodes.GetValueOrDefault(Path.GetFileName(name));
        }
        public List<EpisodeInfo> GetEpisodes()
        {
            return Directory.GetDirectories(baseDir)
                            .Select(e => new EpisodeInfo(e))
                            .ToList();
        }
        public EpisodeInfo New()
        {
            var dir = Utils.GetUnusedDirName(Path.Combine(baseDir, "Episode"));
            Directory.CreateDirectory(dir);
            var info = new EpisodeInfo(dir);
            episodes[Path.GetFileName(dir)] = info;
            return info;
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

    public class EpisodeInfo
    {
        public SceneManager smanager;
        public string path;
        public string name
        {
            get => Path.GetFileName(path)
                       .Replace(" ", "_")
                       .Replace("　", "_")
                       .Replace("\t", "_");
        }
        public EpisodeInfo(string path)
        {
            this.path = path;
            smanager = new SceneManager(path);
        }
        public ImageSource? GetThumbnail()
        {
            return smanager.scenes.First().Value.thumbnail;
        }
        public void Rename(string name)
        {
            var dest = Path.Combine(path, Path.Combine(Path.GetDirectoryName(path), name));
            File.Move(path, dest);
            path = dest;
        }
    }
}
