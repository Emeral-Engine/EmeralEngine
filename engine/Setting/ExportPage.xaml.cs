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
using System.Xml.Linq;

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
            };
        }

        private void ContentFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ContentFormat = ContentFormat.Text;
            Coloring(ContentFormat);
        }

        private void SceneFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.SceneFormat = SceneFormat.Text;
            Coloring(SceneFormat);
        }

        private void ScriptFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.pmanager.Project.ExportSettings.ScriptFormat = ScriptFormat.Text;
            Coloring(ScriptFormat);
        }

        private void Coloring(Xceed.Wpf.Toolkit.RichTextBox tbox)
        {
            if (_IsHandling) return;
            _IsHandling = true;
            var doc = tbox.Document;
            if (doc is null) return;
            new TextRange(doc.ContentStart, doc.ContentEnd)
                .ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
            var text = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
            var pattern = "(" + string.Join("|",
                HILIGHTED_KWD.Select(Regex.Escape)) + ")";
            foreach (Match m in Regex.Matches(text, pattern))
            {
                var start = GetTextPointerAtOffset(doc.ContentStart, m.Index);
                var end = GetTextPointerAtOffset(doc.ContentStart, m.Index + m.Length);
                if (start != null && end != null)
                {
                    new TextRange(start, end)
                        .ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Purple);
                }
            }
            _IsHandling = false;
        }

        private TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
        {
            var navigator = start;
            int count = 0;

            while (navigator != null)
            {
                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string run = navigator.GetTextInRun(LogicalDirection.Forward);

                    if (count + run.Length >= offset)
                    {
                        return navigator.GetPositionAtOffset(offset - count);
                    }

                    count += run.Length;
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
