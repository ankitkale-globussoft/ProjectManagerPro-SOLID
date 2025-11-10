using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProjectManagerPro_SOLID.Views
{
    /// <summary>
    /// Interaction logic for AddTaskWindow.xaml
    /// </summary>
    public partial class AddTaskWindow : Window
    {
        public AddTaskWindow(User currentUser, Project project)
        {
            InitializeComponent();
            DataContext = new AddTaskViewModel(currentUser, project, this);
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}