using ProjectManagerPro_SOLID.DirectoryUtilities;
using ProjectManagerPro_SOLID.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectManagerPro_SOLID
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentUsername;
        public MainWindow()
        {
            InitializeComponent();
            var info = DirHelper.GetUser();

            _currentUsername = info?.Username ?? "Username *not found*";
            MainFrame.Navigate(new LoginView(info));
        }
    }
}