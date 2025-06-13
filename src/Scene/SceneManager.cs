using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using EmeralEngine.Core;
using EmeralEngine.Script;

namespace EmeralEngine.Scene
{
    public class SceneManager : Manager<SceneInfo>
    {
        public SortedList<int, SceneInfo> scenes;
        private string episodeDir;
        public SceneManager(string dir)
        {
            episodeDir = dir;
            Load();
        }
        private void Load()
        {
            scenes = new();
            foreach (var s in Directory.GetFiles(episodeDir, "*.es"))
            {
                var info = Analyzer.FromFile(s);
                scenes.Add(info.order, info);
            }
            if (scenes.Count == 0)
            {
                New();
            }
        }
        public void Dump(string dir="")
        {
            foreach (var s in scenes)
            {
                s.Value.Dump(dir.Length > 0 ? Path.Combine(dir, Path.GetFileName(s.Value.path)) : "");
            }
        }
        public SceneInfo New()
        {
            var path = Path.Combine(episodeDir, "1.es");
            if (File.Exists(path))
            {
                path = Utils.GetUnusedFileName(Path.Combine(episodeDir, ".es"));
            }
            var info = new SceneInfo()
            {
                path = path,
                order = scenes.Count + 1
            };
            info.AddScript();
            if (scenes.Count > 0)
            {
                var s = scenes.Last();
                info.bg = s.Value.bg;
                info.bgm = s.Value.bgm;
                info.msw = s.Value.msw;
            }
            info.Dump();
            scenes.Add(info.order, info);
            return info;
        }
    }

    public class SceneInfo : BaseInfo
    {
        public BitmapImage? thumbnail;
        public string processed_memo;
        private string _path, _bgm, _bg;
        public List<ScriptInfo> scripts;
        private int _order, _msw;
        public string memo
        {
            set
            {
                processed_memo = value.Replace("*/", @"\*/");
            }
            get
            {
                if (processed_memo is null)
                {
                    return "";
                }
                else
                {
                    return processed_memo.Replace(@"\*/", "*/");
                }
            }
        }
        public string bgm
        {
            set
            {
                if (File.Exists(MainWindow.pmanager.GetResource(value)))
                {
                    _bgm = MainWindow.pmanager.RelToResourcePath(value);
                }
                else
                {
                    _bgm = "";
                }
            }
            get => _bgm ?? "";
        }
        public string bg
        {
            set
            {
                if (string.IsNullOrEmpty(value) || !File.Exists(MainWindow.pmanager.GetResource(value)))
                {
                    _bg = "";
                }
                else
                {
                    _bg = MainWindow.pmanager.RelToResourcePath(value);
                    thumbnail = Utils.CreateBmp(MainWindow.pmanager.GetResource(_bg));
                }
            }
            get => _bg ?? "";
        }
        public int msw
        {
            set
            {
                _msw = value;
            }
            get => _msw;
        }
        public int order
        {
            set
            {
                _order = value;
            }
            get => _order;
        }
        private string script
        {
            get
            {
                var res = "";
                foreach (var s in scripts)
                {
                    res += $"""
                        script: -"{s.speaker}" "{string.Join("\" \"", s.charas)}"
                        {s.processed_script}
                        :script
                        """ + "\r\n";
                }
                return res;
            }
        }
        public SceneInfo()
        {
            scripts = new();
        }
        public void AddScript(string script, string speaker, List<string> charas)
        {
            scripts.Add(new ScriptInfo()
            {
                charas = charas,
                script = script,
                speaker = speaker
            });
        }
        public void AddScript(int idx = -1)
        {
            if (idx < 0)
            {
                scripts.Add(new ScriptInfo());
            }
            else
            {
                scripts.Insert(idx, new ScriptInfo());
            }
        }

        public void Dump(string to = "")
        {
            File.WriteAllText(to.Length > 0 ? to : path, $"""
                order: {order}
                trans: {trans} {trans_color} {fadeout} {fadein}
                interval: {interval}
                msw: {msw}
                bgm: {bgm}
                bg: {bg}
                /*
                {processed_memo}
                */
                {script}
                """);
        }
    }
}