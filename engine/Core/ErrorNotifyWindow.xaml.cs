using System.Windows;

namespace EmeralEngine.Notify
{
    /// <summary>
    /// ErrorNotifyWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ErrorNotifyWindow : Window
    {
        public ErrorNotifyWindow()
        {
            InitializeComponent();
        }

        public static void Show(Window parent, string err)
        {
            var w = new ErrorNotifyWindow();
            w.Owner = parent;
            w.Log.Text = err;
            w.ShowDialog();
        }

        public static void Show(string err)
        {
            var w = new ErrorNotifyWindow();
            w.Log.Text = err;
            w.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Log.Text);
        }
    }
}
