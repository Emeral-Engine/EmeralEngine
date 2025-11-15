using System.Windows.Controls;

namespace EmeralEngine.TitleScreen
{
    /// <summary>
    /// NormalTitlePage.xaml の相互作用ロジック
    /// </summary>
    public partial class NormalTitlePage : Page
    {
        private TitleScreenDesigner window;
        public NormalTitlePage(TitleScreenDesigner w)
        {
            InitializeComponent();
            window = w;
        }
    }
}
