using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace EmeralEngine.Project
{
    /// <summary>
    /// ProjectLoadingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ProjectLoadingWindow : Window
    {
        private readonly Action loadAction;
        private bool IsFinished;
        private ExceptionDispatchInfo? exception;

        public ProjectLoadingWindow(Window owner, Action action)
        {
            InitializeComponent();
            Owner = owner;
            loadAction = action;
            Closing += (sender, e) =>
            {
                e.Cancel = !IsFinished;
            };
            Loaded += OnLoaded;
        }

        public void Start()
        {
            ShowDialog();
            exception?.Throw();
        }

        public void SetProgress(string description, int current, int maximum)
        {
            Description.Content = description;
            Progress.Maximum = maximum;
            Progress.Value = current;
            Percent.Content = $"{(int)((double)current / maximum * 100)}%";
            Refresh();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    loadAction();
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    IsFinished = true;
                    Close();
                }
            }, DispatcherPriority.Background);
        }

        private void Refresh()
        {
            var frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
