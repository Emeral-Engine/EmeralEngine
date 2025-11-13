using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmeralEngine.Setting
{
    /// <summary>
    /// ExportPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ExportPage : Page
    {
        private static string[] CONTENT_HILIGHTED_KWD = { "%(n)", "%(scenes)"};
        private static string[] SCENE_HILIGHTED_KWD = { "%(bg)", "%(bgm)", "%(scripts)", "%(fadein)", "%(fadeout)", "%(wait)"};
        private static string[] SCRIPT_HILIGHTED_KWD = { "%(pictures)", "%(speaker)", "%(script)"};
        private bool _IsHandling;
        private Regex ContentPat, ScenePat, ScriptPat;
        public ExportPage()
        {
            InitializeComponent();
            ContentPat = new Regex("(" + string.Join("|", CONTENT_HILIGHTED_KWD.Select(Regex.Escape)) + ")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            ScenePat = new Regex("(" + string.Join("|", SCENE_HILIGHTED_KWD.Select(Regex.Escape)) + ")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            ScriptPat = new Regex("(" + string.Join("|", SCRIPT_HILIGHTED_KWD.Select(Regex.Escape)) + ")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _IsHandling = true;
            var doc = new FlowDocument();
            var p = new Paragraph();
            p.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ContentFormat);
            doc.Blocks.Add(p);
            ContentFormat.Document = doc;
            var doc1 = new FlowDocument();
            var p1 = new Paragraph();
            p1.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.SceneFormat);
            doc1.Blocks.Add(p1);
            SceneFormat.Document = doc1;
            var doc2 = new FlowDocument();
            var p2 = new Paragraph();
            p2.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ScriptFormat);
            doc2.Blocks.Add(p2);
            ScriptFormat.Document = doc2;
            _IsHandling = false;
            Coloring(ContentFormat, ContentPat);
            Coloring(SceneFormat, ScenePat);
            Coloring(ScriptFormat, ScriptPat);
        }

        private void ContentFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ContentFormat = new TextRange(
                ContentFormat.Document.ContentStart,
                ContentFormat.Document.ContentEnd
            ).Text;
            Coloring(ContentFormat, ContentPat);
        }

        private void SceneFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.SceneFormat = new TextRange(
                SceneFormat.Document.ContentStart,
                SceneFormat.Document.ContentEnd
            ).Text;
            Coloring(SceneFormat, ScenePat);
        }

        private void ScriptFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ScriptFormat = new TextRange(
                ScriptFormat.Document.ContentStart,
                ScriptFormat.Document.ContentEnd
            ).Text;
            Coloring(ScriptFormat, ScriptPat);
        }

        private void Coloring(RichTextBox tbox, Regex r)
        {
            if (_IsHandling) return;
            _IsHandling = true;

            var selection = tbox.Selection;
            var selStart = selection.Start;
            var selEnd = selection.End;
            var caretPosition = tbox.CaretPosition;
            string text = GetPlainText(tbox.Document);
            var fullRange = new TextRange(tbox.Document.ContentStart, tbox.Document.ContentEnd);
            fullRange.ClearAllProperties();
            fullRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);

            foreach (Match m in r.Matches(text))
            {
                int matchStart = m.Index;
                int matchLength = m.Length;

                var startPtr = GetTextPointerAtTextOffset(tbox.Document.ContentStart, matchStart);
                if (startPtr == null) continue;
                var endPtr = GetTextPointerAtTextOffset(startPtr, matchLength); // start from startPtr for speed
                if (endPtr == null) continue;
                var range = new TextRange(startPtr, endPtr);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Purple);
            }
            try
            {
                tbox.Selection.Select(selStart, selEnd);
                tbox.CaretPosition = caretPosition;
            }
            catch
            {
                tbox.CaretPosition = tbox.Document.ContentEnd;
            }
            finally
            {
                _IsHandling = false;
            }
        }

        private static string GetPlainText(FlowDocument doc)
        {
            return new TextRange(doc.ContentStart, doc.ContentEnd).Text;
        }

        private static TextPointer GetTextPointerAtTextOffset(TextPointer start, int textOffset)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (textOffset < 0) throw new ArgumentOutOfRangeException(nameof(textOffset));

            var navigator = start;
            int remaining = textOffset;

            while (navigator != null && navigator.CompareTo(start.DocumentEnd) < 0)
            {
                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string runText = navigator.GetTextInRun(LogicalDirection.Forward);
                    if (runText.Length >= remaining)
                    {
                        return navigator.GetPositionAtOffset(remaining);
                    }
                    else
                    {
                        remaining -= runText.Length;
                        navigator = navigator.GetPositionAtOffset(runText.Length);
                    }
                }
                else
                {
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return remaining == 0 ? start.DocumentEnd : null;
        }
    }
}
