using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EmeralEngine.Scene
{
    internal class Analyzer
    {
        private static string[] LINE_CHAR = new string[2] { "\r\n", "\n" };
        public static SceneInfo Analyze(string code)
        {
            var info = new SceneInfo();
            var line_flag = false;
            var memo_flag = false;
            var script = "";
            var speaker = "";
            var charas = new List<string>();
            var memo = "";
            foreach (var c in code.Split(LINE_CHAR, StringSplitOptions.None))
            {
                var cmd = c.Trim().Split(" ").ToList();
                var args = cmd[1..];
                var kwd = cmd[0];
                if (kwd == "script:" && !line_flag)
                {
                    line_flag = true;
                    for (; 0 < args.Count;)
                    {
                        var v = GetArg(args);
                        if (speaker == "" && v.StartsWith('-'))
                        {
                            speaker = v.TrimStart('-').TrimStart('"');
                        }
                        else if (!string.IsNullOrWhiteSpace(v))
                        {
                            charas.Add(v);
                        }
                    }
                }
                else if (kwd == ":script")
                {
                    if (line_flag)
                    {
                        line_flag = false;
                        info.AddScript(script.TrimEnd().Replace(@"\:script", ":script"), speaker, charas, false);
                        script = "";
                        charas = new();
                        speaker = "";
                    }
                    else
                    {
                        RaiseError(":scriptキーワードの前にscript:キーワードが必要です");
                    }
                }
                else if (line_flag)
                {
                    script += c + "\n";
                }
                else if (kwd == "/*")
                {
                    memo_flag = true;
                }
                else if (kwd == "*/")
                {
                    if (memo_flag)
                    {
                        memo_flag = false;
                        info.memo = memo.TrimEnd().Replace(@"\*/", "*/");
                    }
                    else
                    {
                        RaiseError("*/の前に/*が必要です");
                    }
                }
                else if (memo_flag)
                {
                    memo += c + "\n";
                }
                else if (kwd == "msw:")
                {
                    var n = GetArg(args);
                    try
                    {
                        info.msw = int.Parse(n);
                    }
                    catch (FormatException)
                    {
                        RaiseError($"{n}は数字ではありません");
                    }
                    if (0 < args.Count)
                    {
                        RaiseError("msw:キーワードの引数が多すぎます");
                    }
                }
                else if (kwd == "bgm:")
                {
                    info.bgm = GetArg(args);
                    if (0 < args.Count)
                    {
                        RaiseError("bgm:キーワードの引数が多すぎます");
                    }
                }
                else if (kwd == "bg:")
                {
                    info.bg = GetArg(args);
                    if (0 < args.Count)
                    {
                        RaiseError("bg:キーワードの引数が多すぎます");
                    }
                }
                else if (kwd == "order:")
                {
                    var n = GetArg(args);
                    try
                    {
                        info.order = int.Parse(n);
                    }
                    catch (FormatException)
                    {
                        RaiseError($"{n}は数字ではありません");
                    }
                    if (0 < args.Count)
                    {
                        RaiseError("order:キーワードの引数が多すぎます");
                    }
                }
                else if (kwd == "interval:")
                {
                    var n = GetArg(args);
                    try
                    {
                        info.interval = int.Parse(n);
                    }
                    catch (FormatException)
                    {
                        RaiseError($"{n}は数字ではありません");
                    }
                    if (0 < args.Count)
                    {
                        RaiseError("interval:キーワードの引数が多すぎます");
                    }
                }
                else if (kwd == "trans:")
                {
                    var n = "";
                    try
                    {
                        n = GetArg(args);
                        info.trans = int.Parse(n);
                        info.trans_color = GetArg(args);
                        n = GetArg(args);
                        info.fadeout = double.Parse(n);
                        n = GetArg(args);
                        info.fadein = double.Parse(n);
                    }
                    catch (FormatException)
                    {
                        RaiseError($"{n}は数字ではありません");
                    }
                    if (0 < args.Count)
                    {
                        RaiseError("trans:キーワードの引数が多すぎます");
                    }
                }
            }
            return info;
        }

        private static void RaiseError(string err)
        {
            MessageBox.Show(err);
            App.Current.Shutdown();
        }

        private static string GetArg(List<string> args)
        {
            var isContinue = false;
            var res = "";
            for (; 0 < args.Count;)
            {
                var v = args[0];
                if (v.StartsWith("\"") || v.StartsWith("-\""))
                {
                    isContinue = true;
                }
                if (v.EndsWith("\""))
                {
                    if (isContinue)
                    {
                        res += v.Trim('"');
                        args.RemoveAt(0);
                        break;
                    }
                    else
                    {
                        RaiseError($"\"で囲めていません: {v}");
                    }
                }
                if (isContinue)
                {
                    res += v.Trim('"');
                    args.RemoveAt(0);
                }
                else
                {
                    res = v;
                    args.RemoveAt(0);
                    break;
                }
            }
            return res;
        }
        public static SceneInfo FromFile(string file)
        {
            var info = Analyze(File.ReadAllText(file));
            info.path = file;
            return info;
        }
    }
}