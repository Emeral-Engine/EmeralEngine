using EmeralEngine.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EmeralEngine.Resource
{
    /// <summary>
    /// MovieViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class MovieViewer : Window
    {
        private double defaultWidth, defaultHeight;
        private bool IsDraggingSlider;
        private DispatcherTimer hideSliderTimer, updateSliderTimer;
        public MovieViewer(string path)
        {
            InitializeComponent();
            defaultWidth = Width;
            defaultHeight = Height;
            Player.Source = new Uri(Path.GetFullPath(path));
            hideSliderTimer = new()
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            hideSliderTimer.Tick += (sender, e) =>
            {
                hideSliderTimer.Stop();
                updateSliderTimer.Stop();
                ToolBg.Opacity = 0;
                MovieSlider.Visibility = Visibility.Collapsed;
                Cursor = Cursors.None;
            };
            updateSliderTimer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            updateSliderTimer.Tick += (sender, e) =>
            {
                if (!IsDraggingSlider)
                {
                    MovieSlider.Maximum = Utils.GetAsSec(Player.NaturalDuration);
                    MovieSlider.Value = Utils.GetAsSec(Player.Position);
                }
            };
        }

        public static void View(string path)
        {
            var w = new MovieViewer(path);
            w.MovieSlider.Value = 0;
            w.Player.Play();
            w.ShowDialog();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var r = Math.Min(ActualWidth / defaultWidth, ActualHeight / defaultHeight);
            Scale.ScaleX = r;
            Scale.ScaleY = r;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDraggingSlider)
            {
                hideSliderTimer.Stop();
                updateSliderTimer.Stop();
                MovieSlider.Value = Utils.GetAsSec(Player.Position);
                ToolBg.Opacity = 0.5;
                MovieSlider.Visibility = Visibility.Visible;
                Cursor = Cursors.Arrow;
                hideSliderTimer.Start();
                updateSliderTimer.Start();
            }
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsDraggingSlider)
            {
                Player.Position = TimeSpan.FromSeconds(MovieSlider.Value);
            }
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            IsDraggingSlider = true;
            hideSliderTimer.Stop();
            updateSliderTimer.Stop();
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsDraggingSlider = false;
            Player.Position = TimeSpan.FromSeconds(MovieSlider.Value);
            hideSliderTimer.Start();
            updateSliderTimer.Start();
        }
    }
}
