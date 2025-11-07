using System.Windows;

namespace EmeralEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e){
            var f = e.Args.Length == 0;
            var w = new MainWindow(Default: f);
            w.Show();
            if (!f)
            {
                w.LoadProject(e.Args[0]);
            }
        }
    }

}
