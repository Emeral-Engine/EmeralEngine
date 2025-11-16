using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// ExportPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ExportPage : Page
    {
        private static string[] HIGHLIGHTED_KWD = { "%(n1)", "%(n2)", "%(n3)", "%(n4)", "%(scenes)", "%(bg)", "%(bgm)", "%(scripts)", "%(fadein)", "%(fadeout)", "%(wait)", "%(pictures)", "%(speaker)", "%(script)", "%(picture)", "%(picturesn)"};
        private static Regex HighLightPat = new Regex($"({(string.Join("|", HIGHLIGHTED_KWD.Select(Regex.Escape)))})");
        private static Regex LinePat = new Regex(@"\r\n$");
        private bool _IsHandling;
        public ExportPage()
        {
            InitializeComponent();
            _IsHandling = true;
            var doc = new FlowDocument();
            var p = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ContentFormat);
            doc.Blocks.Add(p);
            ContentFormat.Document = doc;
            ContentFormat.PreviewKeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter)
                {
                    e.Handled = true;
                    var caret = ContentFormat.CaretPosition;
                    caret.InsertTextInRun("\r\n");
                    var offset = GetCaretOffset(ContentFormat);
                    if (offset != 0)
                    {
                        ContentFormat.CaretPosition = caret.GetPositionAtOffset(offset, LogicalDirection.Forward);
                    }
                    ContentFormat.ScrollToVerticalOffset(ContentFormat.VerticalOffset + 16);
                }
            };
            var doc1 = new FlowDocument();
            var p1 = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p1.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.SceneFormat);
            doc1.Blocks.Add(p1);
            SceneFormat.Document = doc1;
            SceneFormat.PreviewKeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter)
                {
                    e.Handled = true;
                    var caret = SceneFormat.CaretPosition;
                    caret.InsertTextInRun("\r\n");
                    var offset = GetCaretOffset(SceneFormat);
                    if (offset != 0)
                    {
                        SceneFormat.CaretPosition = caret.GetPositionAtOffset(offset, LogicalDirection.Forward);
                    }
                    SceneFormat.ScrollToVerticalOffset(SceneFormat.VerticalOffset + 16);
                }
            };
            var doc2 = new FlowDocument();
            var p2 = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p2.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ScriptFormat);
            doc2.Blocks.Add(p2);
            ScriptFormat.Document = doc2;
            ScriptFormat.PreviewKeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter)
                {
                    e.Handled = true;
                    var caret = ScriptFormat.CaretPosition;
                    caret.InsertTextInRun("\r\n");
                    var offset = GetCaretOffset(ScriptFormat);
                    if (offset == 0)
                    {
                        ScriptFormat.CaretPosition = caret.GetPositionAtOffset(offset, LogicalDirection.Forward);
                    }
                    ScriptFormat.ScrollToVerticalOffset(ScriptFormat.VerticalOffset + 16);
                }
            };
            var doc3 = new FlowDocument();
            var p3 = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p3.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.PictureFormat);
            doc3.Blocks.Add(p3);
            PictureFormat.Document = doc3;
            PictureFormat.PreviewKeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter)
                {
                    e.Handled = true;
                    var caret = PictureFormat.CaretPosition;
                    caret.InsertTextInRun("\r\n");
                    var offset = GetCaretOffset(PictureFormat);
                    if (offset != 0)
                    {
                        PictureFormat.CaretPosition = caret.GetPositionAtOffset(offset, LogicalDirection.Forward);
                    }
                    PictureFormat.ScrollToVerticalOffset(PictureFormat.VerticalOffset + 16);
                }
            };
            BeginChar.Text = MainWindow.pmanager.Project.ExportSettings.BeginChar;
            EndChar.Text = MainWindow.pmanager.Project.ExportSettings.EndChar;
            IsEscape.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsEscape;
            IsIndented.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsIndented;
            ContentsSeparator.Text = MainWindow.pmanager.Project.ExportSettings.ContentsSeparator;
            ScenesSeparator.Text = MainWindow.pmanager.Project.ExportSettings.ScenesSeparator;
            ScriptsSeparator.Text = MainWindow.pmanager.Project.ExportSettings.ScriptsSeparator;
            PicturesSeparator.Text = MainWindow.pmanager.Project.ExportSettings.PicturesSeparator;
            _IsHandling = false;
            Loaded += (sender, e) =>
            {
                Coloring(ContentFormat);
                Coloring(SceneFormat);
                Coloring(ScriptFormat);
                Coloring(PictureFormat);
            };
        }

        private int GetCaretOffset(RichTextBox tbox)
        {
            var caret = tbox.CaretPosition;
            if (caret.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
            {
                var run = caret.Parent as Run;
                if (run is not null)
                {
                    caret = caret.GetInsertionPosition(LogicalDirection.Forward);
                    var lineStart = caret.GetLineStartPosition(0);
                    int count;
                    var lineEnd = caret.GetLineStartPosition(1, out count);
                    if (lineEnd == null)
                    {
                        lineEnd = tbox.Document.ContentEnd;
                    }
                    var range = new TextRange(lineStart, lineEnd);
                    if (range.Text.TrimEnd('\r', '\n').Length == 0) return 0;
                    var color = run.Foreground as SolidColorBrush;
                    if (color is not null)
                    {
                        return color.Color == Colors.Black ? 1 : 3;
                    }
                }
            }
            return 1;
        }

        private void ContentFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ContentFormat = LinePat.Replace(new TextRange(
                                                                           ContentFormat.Document.ContentStart,
                                                                           ContentFormat.Document.ContentEnd
                                                                       ).Text, "");
            Coloring(ContentFormat);
        }

        private void SceneFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.SceneFormat = LinePat.Replace(new TextRange(
                                                                           SceneFormat.Document.ContentStart,
                                                                           SceneFormat.Document.ContentEnd
                                                                       ).Text, "");
            Coloring(SceneFormat);
        }

        private void ScriptFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ScriptFormat = LinePat.Replace(new TextRange(
                                                                           ScriptFormat.Document.ContentStart,
                                                                           ScriptFormat.Document.ContentEnd
                                                                       ).Text, "");
            Coloring(ScriptFormat);
        }

        private void PictureFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.PictureFormat = LinePat.Replace(new TextRange(
                                                                           PictureFormat.Document.ContentStart,
                                                                           PictureFormat.Document.ContentEnd
                                                                       ).Text, "");
            Coloring(PictureFormat);
        }

        private void Coloring(Xceed.Wpf.Toolkit.RichTextBox tbox)
        {
            if (_IsHandling) return;
            _IsHandling = true;
            var doc = tbox.Document;
            var full = new TextRange(doc.ContentStart, doc.ContentEnd);
            full.ClearAllProperties();
            string text = full.Text;
            foreach (Match m in HighLightPat.Matches(text))
            {
                var start = GetTextPointerAtOffset(doc.ContentStart, m.Index);
                var end = GetTextPointerAtOffset(doc.ContentStart, m.Index + m.Length);
                if (start is not null)
                {
                    if (end is null)
                    {
                        end = doc.ContentEnd;
                    }
                    var range = new TextRange(start, end);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Purple);
                }
            }
            _IsHandling = false;
        }

        public static TextPointer? GetTextPointerAtOffset(TextPointer start, int offset)
        {
            var navigator = start;
            int cnt = 0;

            while (navigator != null)
            {
                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = navigator.GetTextInRun(LogicalDirection.Forward);
                    if (cnt + textRun.Length > offset)
                    {
                        return navigator.GetPositionAtOffset(offset - cnt);
                    }
                    cnt += textRun.Length;
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }

        private void BeginChar_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.BeginChar = BeginChar.Text;
        }

        private void EndChar_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.EndChar = EndChar.Text;
        }

        private void PicturesSeparator_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.PicturesSeparator = PicturesSeparator.Text;
        }

        private void IsBackSlashEscape_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsEscape = (bool)IsEscape.IsChecked;
        }

        private void IsIndented_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsIndented = (bool)IsIndented.IsChecked;
        }

        private void ContentsSeparator_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ContentsSeparator = ContentsSeparator.Text;
        }

        private void ScenesSeparator_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ScenesSeparator = ScenesSeparator.Text;
        }

        private void ScriptsSeparator_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ScriptsSeparator = ScriptsSeparator.Text;
        }
    }
}
