using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static string[] HILIGHTED_KWD = { "%(n1)", "%(n2)", "%(n3)", "%(scenes)", "%(bg)", "%(bgm)", "%(scripts)", "%(fadein)", "%(fadeout)", "%(wait)", "%(pictures)", "%(speaker)", "%(script)"};
        private bool _IsHandling;
        private static Regex LinePat = new Regex(@"\r\n$");
        private Regex HilightPat;
        public ExportPage()
        {
            InitializeComponent();
            HilightPat = new Regex("(" + string.Join("|", HILIGHTED_KWD.Select(Regex.Escape)) + ")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _IsHandling = true;
            var doc = new FlowDocument();
            var p = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ContentFormat);
            doc.Blocks.Add(p);
            ContentFormat.Document = doc;
            var doc1 = new FlowDocument();
            var p1 = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p1.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.SceneFormat);
            doc1.Blocks.Add(p1);
            SceneFormat.Document = doc1;
            var doc2 = new FlowDocument();
            var p2 = new Paragraph()
            {
                Margin = new Thickness(0)
            };
            p2.Inlines.Add(MainWindow.pmanager.Project.ExportSettings.ScriptFormat);
            doc2.Blocks.Add(p2);
            ScriptFormat.Document = doc2;
            BeginChar.Text = MainWindow.pmanager.Project.ExportSettings.BeginChar;
            EndChar.Text = MainWindow.pmanager.Project.ExportSettings.EndChar;
            IsBackSlashEscape.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsBackSlashEscape;
            IsScenesArrayShape.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsScenesArrayShape;
            IsScriptsArrayShape.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsScriptsArrayShape;
            IsPicturesArrayShape.IsChecked = MainWindow.pmanager.Project.ExportSettings.IsPicturesArrayShape;
            PicturesSeparator.IsEnabled = !MainWindow.pmanager.Project.ExportSettings.IsPicturesArrayShape;
            PicturesSeparator.Text = MainWindow.pmanager.Project.ExportSettings.PicturesSeparator;
            _IsHandling = false;
            Coloring(ContentFormat);
            Coloring(SceneFormat);
            Coloring(ScriptFormat);
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

        private void Coloring(RichTextBox tbox)
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

            foreach (Match m in HilightPat.Matches(text))
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

        private void BeginChar_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.BeginChar = BeginChar.Text;
        }

        private void EndChar_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.EndChar = EndChar.Text;
        }

        private void IsScenesArrayShape_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsScenesArrayShape = (bool)IsScenesArrayShape.IsChecked;
        }

        private void IsScriptsArrayShape_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsScriptsArrayShape = (bool)IsScriptsArrayShape.IsChecked;
        }

        private void IsPicturesArrayShape_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsPicturesArrayShape = (bool)IsPicturesArrayShape.IsChecked;
            PicturesSeparator.IsEnabled = !MainWindow.pmanager.Project.ExportSettings.IsPicturesArrayShape;
        }

        private void PicturesSeparator_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.PicturesSeparator = PicturesSeparator.Text;
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

        private void IsBackSlashEscape_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.IsBackSlashEscape = (bool)IsBackSlashEscape.IsChecked;
        }
    }
}
