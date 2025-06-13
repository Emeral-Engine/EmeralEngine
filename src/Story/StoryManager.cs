﻿using EmeralEngine.Core;
using EmeralEngine.Resource.CustomTransition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
                path = path,
                id = n
            };
            stories.Add(n, info);
            return info;
        }
    }

    public class StoryAnalyzer
    {
        public StoryAnalyzer()
        {

        }
    }
    public class ContentInfo : BaseInfo
    {
        public int id {  get; set; }
        private string _path;
        override public string path
        {
            get => _path ?? "";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _path = Path.GetRelativePath(MainWindow.pmanager.ProjectResourceDir, value);
                }
            }
        }
        public string Name {  get; set; }
        public Direction[] direction { get; set; }
        public bool IsScenes()
        {
            return Directory.Exists(path);
        }
        public string GetRelPath()
        {
            return Path.GetRelativePath(MainWindow.pmanager.ProjectResourceDir, path);
        }
        public async Task<dynamic>? GetThumbnail()
        {
            if (IsScenes())
            {
                return (new EpisodeInfo(path)).GetThumbnail();
            }
            else if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            else
            {
                var p = new MediaPlayer()
                {
                    Volume = 0
                };
                p.Open(new Uri(Path.GetFullPath(path)));
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
