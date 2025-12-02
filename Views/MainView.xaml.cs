using MaterialDesignThemes.Wpf.Converters.CircularProgressBar;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectManagerPro_SOLID.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private User _currentUser;
        private Point startPoint;
        private bool isDragging;
        public User CurrentUser { get; private set; }

        public MainView(User user)
        {
            InitializeComponent();
            CurrentUser = user;
            _currentUser = user;
            this.DataContext = new MainViewModel(user);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.ProjectSelectedCommand.Execute(null);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if(sender is ContextMenu menu && menu.DataContext is TaskItem task)
            {
                if(DataContext is MainViewModel vm)
                {
                    bool isOwner = task.CreatedById == vm.CurrentUser.NodeID;

                    if (isOwner)
                    {
                        foreach (var item in menu.Items.OfType<MenuItem>())
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Only {task.CreatedByUsername} (creator) and {task.AssignedTo.Username} (assigned person) are authorized to change status of this task.");
                        return;
                    }
                }
            }
        }

        private async void Task_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is TaskItem task)
            {
                if (DataContext is MainViewModel vm)
                {
                    Application.Current.MainWindow.Opacity = 0.5;
                    var editWindow = new EditTaskWindow(task, vm.CurrentUser);
                    editWindow.Owner = Application.Current.MainWindow;
                    editWindow.ShowDialog();
                    Application.Current.MainWindow.Opacity = 1;
                }
            }
        }

        private void MoveToDo_Click(object sender, RoutedEventArgs e) => UpdateTaskStatus(sender, Models.TaskStatus.ToDo);
        private void MoveInProgress_Click(object sender, RoutedEventArgs e) => UpdateTaskStatus(sender, Models.TaskStatus.InProgress);
        private void MoveInReview_Click(object sender, RoutedEventArgs e) => UpdateTaskStatus(sender, Models.TaskStatus.InReview);
        private void MoveCompleted_Click(object sender, RoutedEventArgs e) => UpdateTaskStatus(sender, Models.TaskStatus.Completed);

        private async void UpdateTaskStatus(object sender, Models.TaskStatus newStatus)
        {
            if(sender is MenuItem menuItem && menuItem.DataContext is TaskItem task)
            {
                if(DataContext is MainViewModel vm)
                {
                    try
                    {
                        await vm.UpdateTaskStatusAsync(task, newStatus);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Failed to update task status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            // record where the mouse started; reset dragging flag
            startPoint = e.GetPosition(null);
            isDragging = false;
        }

        private void ListBoxItem_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = startPoint - currentPosition;

                if(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if(sender is ListBoxItem listBoxItem && listBoxItem.DataContext is TaskItem task)
                    {
                        isDragging = true;
                        var dragData = new DataObject("TaskItem", task);
                        DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);
                        isDragging = false;
                    }
                }
            }
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {

            if (!e.Data.GetDataPresent("TaskItem"))
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("TaskItem") || !(sender is ListBox listBox))
                return;
            e.Effects = DragDropEffects.Move;
            if (!(e.Data.GetData("TaskItem") is TaskItem task)) return;

            if(!(task.CreatedById == _currentUser.NodeID || task.AssignedTo?.NodeID == _currentUser.NodeID))
            {
                MessageBox.Show("You are not authorized to change this task.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Models.TaskStatus newStatus = Models.TaskStatus.ToDo;
            var vm = DataContext as MainViewModel;

            if (vm == null) return;

            if (listBox.ItemsSource == vm.InProgressTasks)
                newStatus = Models.TaskStatus.InProgress;
            else if (listBox.ItemsSource == vm.InReviewTasks)
                newStatus = Models.TaskStatus.InReview;
            else if (listBox.ItemsSource == vm.CompletedTasks)
                newStatus = Models.TaskStatus.Completed;

            try
            {
                await vm.UpdateTaskStatusAsync(task, newStatus);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Failed to update task status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Content = new LoginView(new User());
        }
    }
}
