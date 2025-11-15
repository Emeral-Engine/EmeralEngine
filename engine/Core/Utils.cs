using EmeralEngine.Notify;
using EmeralEngine.Project;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EmeralEngine.Core
{
    public class Utils
    {
        private const int THUMBNAIL_POS = 5; //sec
        static string[] image_exts = new string[] { ".jpg", ".jpeg", ".png", ".ping", ".bmp", ".ico", "tiff", ".tif" };
        static string[] movie_exts = new string[] { ".mp4" };
        static string[] audio_exts = new string[] { ".mp3", ".wave", ".wav", ".aif", ".aiff"};
        static string[] suffix = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
        const int BORDER = 1024;

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

        public async static Task<RenderTargetBitmap> CreateBmp(MediaPlayer p)
        {
            p.ScrubbingEnabled = true;
            while (p.NaturalVideoWidth == 0 && p.NaturalVideoHeight == 0)
            {
                await Task.Delay(500);
            }
            if (THUMBNAIL_POS <= p.NaturalDuration.TimeSpan.TotalSeconds)
            {
                p.Position = TimeSpan.FromSeconds(5);
            }
            var w = p.NaturalVideoWidth;
            var h = p.NaturalVideoHeight;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawVideo(p, new Rect(0, 0, p.NaturalVideoWidth, p.NaturalVideoHeight));
            }
            var b = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            b.Render(dv);
            return b;
        }
        public static bool IsImage(string file)
        {
            return image_exts.Contains(Path.GetExtension(file));
        }
        public static bool IsMovie(string file)
        {
            return movie_exts.Contains(Path.GetExtension(file));
        }
        public static bool IsAudio(string file)
        {
            return audio_exts.Contains(Path.GetExtension(file));
        }
        public static string GetUnusedFileName(string path)
        {
            var c = 1;
            var ext = Path.GetExtension(path);
            var b = path[..(path.Length-ext.Length)];
            var res = $"{b}{c}{ext}";
            while (File.Exists(res))
            {
                c++;
                res = $"{b}{c}{ext}";
            }
            return res;
        }
        public static string GetUnusedDirName(string name)
        {
            var c = 0;
            var res = name;
            while (Directory.Exists(res))
            {
                c++;
                res = $"{name}({c})";
            }
            return res;
        }
        public static string CutString(string s, int cols, bool add_dot=true, int lines=1)
        {
            if (s.Count() <= cols) return s;
            var res = "";
            var max = cols * lines;
            var x = 0;
            var cut = false;
            foreach (var c in s)
            {
                res += c.ToString();
                x++;
                if (max <= res.Count())
                {
                    cut = true;
                    break;
                }
                else if (cols <= x)
                {
                    x = 0;
                    res += "\n";
                }
            }
            return res.TrimEnd() + (cut && add_dot ? "..." : "");
        }

        public static string GetEscapedString(string s)
        {
            return Regex.Escape(s).Replace(@"\.", @".");
        }

        public static string GetFormatFileSize(double s)
        {
            var idx = 0;
            while (BORDER <= s)
            {
                s /= BORDER;
                idx++;
            }
            return $"{Math.Round(s, 1, MidpointRounding.AwayFromZero)}{suffix[idx]}B";
        }
        public static void Swap<T>(List<T> list, int from, int to)
        {
            if (from == to) return;
            if (from < to)
            {
                list.Insert(to + 1, list[from]);
                list.RemoveAt(from);
            }
            else
            {
                list.Insert(to, list[from]);
                list.RemoveAt(from+1);
            }
        }

        public static bool IsEqualList<T>(List<T> list1, List<T> list2)
        {
            if (list1.Count == list2.Count)
            {
                for (int i=0; i<list1.Count; i++)
                {
                    if (!list1[i].Equals(list2[i])) return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static double GetAsSec(Duration d)
        {
            return d.HasTimeSpan ? d.TimeSpan.TotalSeconds : 0;
        }

        public static double GetLeftPos(Canvas c, dynamic u)
        {
            var l = Canvas.GetLeft(u);
            if (l is double.NaN)
            {
                return c.ActualWidth - Canvas.GetRight(u) - u.ActualWidth;
            }
            else
            {
                return l;
            }
        }
        public static double GetRightPos(Canvas c, dynamic u)
        {
            var r = Canvas.GetRight(u);
            if (r is double.NaN)
            {
                return c.ActualWidth - Canvas.GetLeft(u) - u.ActualWidth;
            }
            else
            {
                return r;
            }
        }

        public static double GetBottomPos(Canvas c, dynamic u)
        {
            var b = Canvas.GetBottom(u);
            if (b is double.NaN)
            {
                return c.ActualHeight - Canvas.GetTop(u) - u.ActualHeight;
            }
            else
            {
                return b;
            }
        }
        public static double GetTopPos(Canvas c, dynamic u)
        {
            var t = Canvas.GetTop(u);
            if (t is double.NaN)
            {
                return c.ActualHeight - Canvas.GetBottom(u) - u.ActualHeight;
            }
            else
            {
                return t;
            }
        }

        public static SolidColorBrush GetBrush(string code)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(code);
        }

        public static Color GetColor(string code)
        {
            return (Color)ColorConverter.ConvertFromString(code);
        }

        public static string GetHex(Brush b)
        {
            var color = (b as SolidColorBrush).Color;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static bool RaiseError(Process p)
        {
            if (p.ExitCode == 0)
            {
                return true;
            }
            else
            {
                Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    ErrorNotifyWindow.Show(p.StandardOutput.ReadToEnd());
                }));
                return false;
            }
        }
    }

    public static class ImageUtils
    {
        public static string GetFileName(ImageSource source)
        {
            return Path.GetFileName(GetFilePath(source));
        }

        public static string GetFilePath(ImageSource source)
        {
            if (source is BitmapImage bmp)
            {
                return bmp.UriSource.LocalPath;
            }
            else if (source is BitmapFrame bmpf)
            {
                return bmpf.Decoder.ToString();
            }
            else
            {
                return "";
            }
        }

        public static CroppedBitmap CropTransparentEdges(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            int bytesPerPixel = source.Format.BitsPerPixel / 8;
            int alphaIndex = bytesPerPixel - 1;

            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    byte alpha = pixels[index + alphaIndex];

                    if (alpha > 0)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                return null;
            }

            Int32Rect cropRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return new CroppedBitmap(source, cropRect);
        }

        public static BitmapSource LoadImage(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                return decoder.Frames[0];
            }
        }

        public static void SaveImage(BitmapSource source, string outputPath)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
    }

    public class TempDirectory : IDisposable
    {
        public string path;

        public TempDirectory()
        {
            var remains = TempFile.Cleanup();
            string tempPath = Path.GetTempPath();
            string dirName = Guid.NewGuid().ToString();
            path = Path.Combine(tempPath, dirName);
            Directory.CreateDirectory(path);
            remains.remains.Add(path);
            remains.Write();
        }

        public void Dispose()
        {
            if (Directory.Exists(path))
            {
                GC.Collect();
                try
                {
                    Directory.Delete(path, true);
                }
                catch { }
            }
        }
    }

    public class TempFile: IDisposable
    {
        public string path;
        private  bool _disposed = false;
        public TempFile(string suffix="")
        {
            var remains = Cleanup();
            path = Path.Combine(Path.GetTempFileName(), Guid.NewGuid().ToString() + suffix);
            remains.remains.Add(path);
            remains.Write();
        }

        public static CleanupInfo Cleanup()
        {
            var remain = Path.Combine(ProjectManager.BaseDir, "temp_remains.txt");
            var remains = new List<string>();
            if (File.Exists(remain))
            {
                foreach (var p in File.ReadLines(remain))
                {
                    if (Directory.Exists(p))
                    {
                        try
                        {
                            Directory.Delete(p, true);
                        }
                        catch
                        {
                            remains.Append(p);
                        }
                    }
                    else if (File.Exists(p))
                    {
                        try
                        {
                            File.Delete(p);
                        }
                        catch
                        {
                            remains.Add(p);
                        }
                    }
                }
            }
            return new CleanupInfo()
            {
                path = remain,
                remains = remains
            };
        }

        public void Write(string contents)
        {
            File.WriteAllText(path, contents);
        }
        public void Remove()
        {
            try
            {
                File.Delete(path);
            }
            catch { }
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

    public class CleanupInfo
    {
        public string path;
        public List<string> remains;

        public void Write()
        {
            File.WriteAllLines(path, remains);
        }
    }

    public class ResizableBorder: Border
    {
        private bool _IsSelecting;
        private Thickness BORDER_THICK = new Thickness(3);
        private Thickness ZERO_THICK = new Thickness(0);
        public ResizableBorder()
        {
            BorderBrush = CustomColors.SelectingElement;
            Background = Brushes.Transparent;
            MouseEnter += (sender, e) =>
            {
                BorderThickness = BORDER_THICK;
                e.Handled = true;
            };
            MouseLeave += (sender, e) =>
            {
                if (!_IsSelecting) Release();
            };
        }
        public void Focus()
        {
            BorderThickness = BORDER_THICK;
            _IsSelecting = true;
        }

        public void Release()
        {
            BorderThickness = ZERO_THICK;
            _IsSelecting = false;
        }
    }

    public class TextHelper
    {
        public static double GetTextHeight(string text, TextBlock target)
        {
            var t = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(target.FontFamily.ToString()),
                target.FontSize,
                target.Foreground
                );
            return t.Height;
        }
    }

    public class XamlHelper
    {
        public static Regex SourceRegex = new Regex(@" Source\s*=\s*""([^""]+)""");
        public static string ConvertSourceToRel(string xaml)
        {
            return SourceRegex.Replace(xaml, s =>
            {
                return $" Source=\"{Path.GetRelativePath(MainWindow.pmanager.ProjectResourceDir, s.Groups[1].Value)}\"";
            });
        }

        public static string ConvertSourceToAbs(string xaml)
        {
            return SourceRegex.Replace(xaml, s =>
            {
                return $" Source=\"{Path.Combine(MainWindow.pmanager.ProjectResourceDir, s.Groups[1].Value)}\"";
            });
        }
    }

    public class DesignerElementManager
    {
        public bool Handled;
        private ResizableBorder _border;

        public void Focus(ResizableBorder b)
        {
            if (!Handled)
            {
                if (_border is not null)
                {
                    _border.Release();
                }
                _border = b;
                _border.Focus();
            }
        }
    }

    public class DragResizeHelper
    {
        private const int THICK = 10;
        private const int THICK2 = THICK * 2;
        public static ResizableBorder Make(Canvas parent, FrameworkElement element)
        {
            var isTextBlock = element is TextBlock;
            var hbind = new Binding("ActualHeight")
            {
                Source = element
            };
            var wbind = new Binding("ActualWidth")
            {
                Source = element
            };
            var canvas = new Canvas()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            canvas.SetBinding(FrameworkElement.WidthProperty, wbind);
            canvas.SetBinding(FrameworkElement.HeightProperty, hbind);
            var border = new ResizableBorder()
            {
                Child = canvas
            };
            canvas.Children.Add(element);
            Canvas.SetLeft(border, Canvas.GetLeft(element));
            Canvas.SetTop(border, Canvas.GetTop(element));
            element.ClearValue(Canvas.LeftProperty);
            element.ClearValue(Canvas.TopProperty);
            var left = new Thumb()
            {
                Width = THICK,
                Cursor = Cursors.SizeWE,
                Opacity = 0
            };
            left.SetBinding(FrameworkElement.HeightProperty, hbind);
            var right = new Thumb()
            {
                Width = THICK,
                Cursor = Cursors.SizeWE,
                Opacity = 0
            };
            right.SetBinding(FrameworkElement.HeightProperty, hbind);
            var top = new Thumb()
            {
                Height = THICK,
                Cursor = isTextBlock ? Cursors.Arrow : Cursors.SizeNS,
                Opacity = 0
            };
            top.SetBinding(FrameworkElement.WidthProperty, wbind);
            var bottom = new Thumb()
            {
                Height = THICK,
                Cursor = isTextBlock ? Cursors.Arrow : Cursors.SizeNS,
                Opacity = 0
            };
            bottom.SetBinding(FrameworkElement.WidthProperty, wbind);
            var center = new Thumb()
            {
                Cursor = Cursors.ScrollAll,
                Opacity = 0
            };
            element.Loaded += (sender, e) =>
            {
                if (THICK2 < element.Width)
                {
                    center.Width = element.ActualWidth - THICK2;
                    center.Height = element.ActualHeight - THICK2;
                }
            };
            element.SizeChanged += (sender, e) =>
            {
                if (THICK2 < element.Width)
                {
                    center.Width = Math.Max(0, element.ActualWidth - THICK2);
                    center.Height = Math.Max(0, element.ActualHeight - THICK2);
                }
            };
            left.DragStarted += (sender, e) =>
            {
                Canvas.SetRight(border, Utils.GetRightPos(parent, border));
                border.ClearValue(Canvas.LeftProperty);
            };
            left.DragCompleted += (sender, e) =>
            {
                Canvas.SetLeft(border, Utils.GetLeftPos(parent, border));
                border.ClearValue(Canvas.RightProperty);
            };
            left.DragDelta += (sender, e) =>
            {
                var w = Math.Max(element.ActualWidth - e.HorizontalChange, THICK2);
                element.Width = w;
            };
            Canvas.SetLeft(left, 0);
            Panel.SetZIndex(left, 1);
            right.DragDelta += (sender, e) =>
            {
                var w = Math.Max(element.ActualWidth + e.HorizontalChange, THICK2);
                element.Width = w;
            };
            Canvas.SetRight(right, 0);
            Panel.SetZIndex(right, 1);
            if (element is not TextBlock)
            {
                top.DragStarted += (sender, e) =>
                {
                    Canvas.SetBottom(border, Utils.GetBottomPos(parent, border));
                    border.ClearValue(Canvas.TopProperty);
                };
                top.DragCompleted += (sender, e) =>
                {
                    Canvas.SetTop(border, Utils.GetTopPos(parent, border));
                    border.ClearValue(Canvas.BottomProperty);
                };
                top.DragDelta += (sender, e) =>
                {
                    var h = Math.Max(element.ActualHeight - e.VerticalChange, THICK2);
                    element.Height = h;
                };
                bottom.DragDelta += (sender, e) =>
                {
                    var h = Math.Max(element.ActualHeight + e.VerticalChange, THICK2);
                    element.Height = h;
                };
            }
            Canvas.SetTop(top, 0);
            Panel.SetZIndex(top, 1);
            Canvas.SetBottom(bottom, 0);
            Panel.SetZIndex(bottom, 1);
            center.DragDelta += (sender, e) =>
            {
                Canvas.SetLeft(border, Math.Min(parent.ActualWidth - element.ActualWidth, Math.Max(0, Canvas.GetLeft(border) + e.HorizontalChange)));
                Canvas.SetTop(border, Math.Min(parent.ActualHeight - element.ActualHeight, Math.Max(0, Canvas.GetTop(border) + e.VerticalChange)));
            };
            Canvas.SetLeft(center, THICK);
            Canvas.SetTop(center, THICK);
            Panel.SetZIndex(center, 1);
            canvas.Children.Add(left);
            canvas.Children.Add(right);
            canvas.Children.Add(top);
            canvas.Children.Add(bottom);
            canvas.Children.Add(center);
            return border;
        }
    }
}