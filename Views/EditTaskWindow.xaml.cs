using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.ViewModels;
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
    /// Interaction logic for EditTaskWindow.xaml
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        public EditTaskWindow(TaskItem task, User usr)
        {
            InitializeComponent();
            DataContext = new EditTaskViewModel(task, this, usr);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Opacity = 0.5;
        }
    }
}
