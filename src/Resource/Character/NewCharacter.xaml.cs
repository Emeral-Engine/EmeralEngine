using System.Windows;
using System.Windows.Controls;

namespace EmeralEngine.Resource.Character
{
    /// <summary>
    /// NewCharacter.xaml の相互作用ロジック
    /// </summary>
    public partial class NewCharacter : Window
    {
        private CharacterWindow parent;
        public NewCharacter(CharacterWindow window)
        {
            InitializeComponent();
            parent = window;
            Owner = parent;
            ResizeMode = ResizeMode.NoResize;
            CharacterName.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
            parent.cmanager.NewCharacter(CharacterName.Text);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CreateButton.IsEnabled = CharacterName.Text.Any();
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Close();
                parent.cmanager.NewCharacter(CharacterName.Text);
            }
        }
    }
}
