using EmeralEngine.Core;
using EmeralEngine.Resource;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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

namespace EmeralEngine.TitleScreen
{
    /// <summary>
    /// CustomTitlePage.xaml の相互作用ロジック
    /// </summary>
    public partial class CustomTitlePage : Page
    {
        private string[] BUTTONS = { "スタート", "ロード", "終了"};
        private TitleScreenDesigner window;
        public int SelectingButton;
        public List<UIElement> Buttons;
        public List<string> ButtonSelections;
        public CustomTitlePage(TitleScreenDesigner w)
        {
            InitializeComponent();
            window = w;
            Buttons = new();
            ButtonSelections = BUTTONS.ToList();
            ApplyItems();
        }

        private void ApplyItems()
        {
            window.IsHandling = true;
            ButtonFunc.Items.Clear();
            foreach (var s in ButtonSelections)
            {
                ButtonFunc.Items.Add(s);
            }
            window.IsHandling = false;
        }
        private int GetUnAttachedType()
        {
            for (var i=0; i<3; i++)
            {
                if (!window.ButtonFuncs.ContainsValue(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (2 < Buttons.Count)
            {
                AddButton.IsEnabled = false;
                return;
            }
            var res = ResourceWindow.SelectImage(window);
            if (!string.IsNullOrEmpty(res))
            {
                var img = new Image
                {
                    Source = Utils.CreateBmp(res),
                };
                var btn = new Button()
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Content = img
                };
                var border = DragResizeHelper.Make(window.PreviewArea, btn);
                var h = border.GetHashCode();
                window.ButtonFuncs.Add(h, GetUnAttachedType());
                border.PreviewMouseLeftButtonDown += (sender, e) =>
                {
                    ButtonFunc.IsEnabled = true;
                    window.ElementManager.Focus(border);
                    SelectingButton = h;
                    LimitButtonTypeSelection();
                    ButtonFunc.SelectedIndex = ButtonSelections.IndexOf(ButtonTypes.Get(window.ButtonFuncs[SelectingButton]));
                };
                window.MakeContextMenu(border);
                Canvas.SetLeft(border, 0);
                Canvas.SetTop(border, 0);
                Canvas.SetZIndex(border, 0);
                window.PreviewArea.Children.Add(border);
                Buttons.Add(border);
                if (2 < Buttons.Count)
                {
                    AddButton.IsEnabled = false;
                }
            }
        }

        private void OnButtonFuncChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!window.IsHandling && 0 < ButtonFunc.SelectedIndex)
            {
                window.ButtonFuncs[SelectingButton] = ButtonTypes.Get(ButtonSelections[ButtonFunc.SelectedIndex]);
            }
        }

        public void LimitButtonTypeSelection()
        {
            var v = BUTTONS.ToList();
            foreach (var i in Buttons)
            {
                var h = i.GetHashCode();
                if (h != SelectingButton)
                {
                    v.Remove(ButtonTypes.Get(window.ButtonFuncs[h]));
                }
            }
            ButtonSelections = v;
            ApplyItems();
        }
    }

    public struct ButtonTypes
    {
        public const int START = 0;
        public const int LOAD = 1;
        public const int END = 2;

        public static int Get(string name)
        {
            return name switch
            {
                "スタート" or "StartButton" => START,
                "ロード" or "LoadButton" => LOAD,
                "終了" or "FinishButton" => END
            };
        }

        public static string Get(int n)
        {
            return n switch
            {
                START => "スタート",
                LOAD => "ロード",
                END => "終了"
            };
        }
    }
}
