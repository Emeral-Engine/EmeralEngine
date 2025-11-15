using System.Windows;

namespace EmeralEngine.Resource.Character
{
    /// <summary>
    /// CharacterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CharacterWindow : Window
    {
        public SelectCharacterPage sPage;
        public CharacterManager cmanager;
        private Window parent;
        public string? SelectedPicture;
        public CharacterWindow(Window p)
        {
            InitializeComponent();
            cmanager = new();
            sPage = new(this, ScreenFrame);
            parent = p;
            Owner = parent;
            ScreenFrame.Navigate(sPage);
        }

        public static string? SelectCharacterPicture(Window parent)
        {
            var w = new CharacterWindow(parent);
            w.sPage.SelectMode();
            w.ShowDialog();
            return w.SelectedPicture;
        }
    }
}
