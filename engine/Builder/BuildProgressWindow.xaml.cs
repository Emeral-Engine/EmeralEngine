using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EmeralEngine.Builder
{
    /// <summary>
    /// CompilationProgressWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class BuildProgressWindow : Window
    {
        public BuildProgressWindow()
        {
            InitializeComponent();
            Closing += (sender, e) =>
            {
                if (MainProgress.Value != MainProgress.Maximum)
                {
                    e.Cancel = true;
                }
            };
        }

        public void Refresh()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            Height += 35;
            MainGrid.Height += 35;
        }

        private void OnCollapsed(object sender, RoutedEventArgs e)
        {
            Height -= 35;
            MainGrid.Height -= 35;
        }
    }
}
