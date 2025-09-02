using System.Windows;
using System.Windows.Threading;

namespace EmeralEngine.Project
{
    /// <summary>
    /// CreateNewProjectProgress.xaml の相互作用ロジック
    /// </summary>
    public partial class CreateProjectProgress : Window
    {
        private bool IsFinished;
        private Task start_task;
        private Action end_func;
        private DispatcherTimer timer;
        public CreateProjectProgress()
        {
            InitializeComponent();
            Closing += (sender, e) =>
            {
                e.Cancel = !IsFinished;
            };
            timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            timer.Tick += (sender, e) =>
            {
                if (start_task.Status != TaskStatus.Running)
                {
                    IsFinished = true;
                    timer.Stop();
                    end_func();
                    Close();
                }
            };
        }
        public void Start(Task task1, Action task2)
        {
            start_task = task1;
            end_func = task2;
            start_task.Start();
            timer.Start();
            ShowDialog();
        }
    }
}