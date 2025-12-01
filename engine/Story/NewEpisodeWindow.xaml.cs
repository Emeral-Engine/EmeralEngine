using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmeralEngine.Story
{
    /// <summary>
    /// NewEpisodeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewEpisodeWindow : Window
    {
        public EpisodeInfo New;
        private EpisodeManager emanager;
        public NewEpisodeWindow(Window w)
        {
            InitializeComponent();
            Owner = w;
            emanager = new();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Create();
        }

        private void Create()
        {
            if (EpisodeName.Text != "")
            {
                Close();
                New = emanager.New(EpisodeName.Text);
            }
        }

        private void EpisodeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            CreateButton.IsEnabled = EpisodeName.Text.Any();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Create();
            }
        }
    }
}
