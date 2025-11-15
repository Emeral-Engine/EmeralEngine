using EmeralEngine.Core;
using System.IO;
using System.Windows.Media;

namespace EmeralEngine.Story
{
    public class StoryManager
    {
        public SortedList<int, ContentInfo> stories;
        public ContentInfo[] StoryInfos
        {
            get => stories.Select(s => s.Value)
                          .ToArray();
        }
        public StoryManager()
        {
            stories = new();
            foreach (var s in MainWindow.pmanager.Project.Story)
            {
                stories.Add(s.id, s);
            }
        }
        public ContentInfo New()
        {
            var n = stories.Count + 1;
            var info = new ContentInfo()
            {
                id = n
            };
            stories.Add(n, info);
            return info;
        }
        public ContentInfo New(string path)
        {
            var n = stories.Count + 1;
            var info = new ContentInfo()
            {
                Path = path,
                id = n
            };
            stories.Add(n, info);
            return info;
        }
    }

    public class ContentInfo : BaseInfo
    {
        public int id {  get; set; }
        private string _path;
        override public string Path
        {
            get => _path ?? "";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _path = System.IO.Path.IsPathRooted(value) ? System.IO.Path.GetRelativePath(MainWindow.pmanager.Temp.path, value) : value;
                }
            }
        }
        public string FullPath
        {
            get => System.IO.Path.Combine(MainWindow.pmanager.Temp.path, _path);
        }
        public string Name {  get; set; }
        public Direction[] Directions { get; set; }
        public bool IsScenes()
        {
            return Directory.Exists(FullPath);
        }

        public string GetRelPathToResource()
        {
            return System.IO.Path.GetRelativePath(MainWindow.pmanager.ProjectResourceDir, FullPath);
        }
        public async Task<dynamic>? GetThumbnail()
        {
            if (IsScenes())
            {
                return (new EpisodeInfo(FullPath)).GetThumbnail();
            }
            else if (string.IsNullOrEmpty(FullPath))
            {
                return null;
            }
            else
            {
                var p = new MediaPlayer()
                {
                    Volume = 0
                };
                p.Open(new Uri(FullPath));
                return await Utils.CreateBmp(p);
            }
        }
    }
    public class Direction
    {
        public string flag {  get; set; }
        public int to {  get; set; } // next id
        public int pre {  get; set; } // id from this
    }
}
