using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.Views;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using PMTaskStatus = ProjectManagerPro_SOLID.Models.TaskStatus;

namespace ProjectManagerPro_SOLID.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Project> projects;

        [ObservableProperty]
        private Project selectedProject;

        partial void OnSelectedProjectChanged(Project value)
        {
            _ = LoadTaskForProject();
        }

        [ObservableProperty]
        private ObservableCollection<Comment> comments = new();

        [ObservableProperty]
        private string? commentText;

        [ObservableProperty]
        private Visibility loading = Visibility.Collapsed;
        [ObservableProperty]
        private ObservableCollection<TaskItem> toDoTasks;

        [ObservableProperty]
        private ObservableCollection<TaskItem> inProgressTasks;

        [ObservableProperty]
        private ObservableCollection<TaskItem> inReviewTasks;

        [ObservableProperty]
        private ObservableCollection<TaskItem> completedTasks;

        public User CurrentUser { get; set; }

        public string Username => CurrentUser?.Username ?? "User";

        public UserRole UserRole => CurrentUser.Role;

        private string _currentListeningProjectID;

        private string _currentListeningProjectIDForComments;

        private HashSet<string> _seenTaskIds = new();


        public MainViewModel(User user)
        {
            CurrentUser = user;
            LoadProjects();
        }

        [RelayCommand]
        private async void AddProject()
        {
            try
            {
                Application.Current.MainWindow.Opacity = 0.5;
                var projectName = Microsoft.VisualBasic.Interaction.InputBox("Enter Project Name", "Add Project");
                if (!string.IsNullOrEmpty(projectName))
                {
                    var addProject = new Project { ProjectID = Guid.NewGuid().ToString(), Name = projectName };
                    await FirebaseHelper.AddProject(addProject);
                    await LoadProjects();
                }
            }
            finally
            {
                Application.Current.MainWindow.Opacity = 1;
            }
        }

        [RelayCommand]
        private async void ProjectSelected()
        {
            try
            {
                if(SelectedProject != null)
                {
                    Application.Current.MainWindow.Opacity = 0.5;
                    Loading = Visibility.Visible;
                    await LoadTaskForProject();
                    await LoadPriorityChartData();
                    // load the task and other config, for the selected project
                }
            }
            finally
            {
                Application.Current.MainWindow.Opacity = 1;
                Loading = Visibility.Collapsed;
            }
        }

        [RelayCommand]
        private async void AddTask()
        {
            Application.Current.MainWindow.Opacity = 0.5;
            try
            {
                if (SelectedProject == null) return;

                var taskWindow = new AddTaskWindow(CurrentUser, SelectedProject);
                taskWindow.Owner = Application.Current.MainWindow;
                taskWindow.Show();
            }
            finally
            {
                Application.Current.MainWindow.Opacity = 1;
            }
        }

        private async Task LoadProjects()
        {
            Application.Current.MainWindow.Opacity = 0.5;
            Loading = Visibility.Visible;

            var AllProjects = await FirebaseHelper.GetProjects();
            Projects = new ObservableCollection<Project>(AllProjects);

            if (Projects.Any())
            {
                SelectedProject = Projects.FirstOrDefault();

                FirebaseHelper.ListenToComments(null, SelectedProject.ProjectID, commentsList =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Comments.Clear();
                        foreach (var comment in commentsList.OrderByDescending(c => c.Timestamp))
                        {
                            Comments.Add(comment);
                        }
                    });
                });
            }
            else
            {
                Application.Current.MainWindow.Opacity = 1;
                Loading = Visibility.Collapsed;
            }
        }

        private async Task LoadTaskForProject()
        {
            if (SelectedProject != null && SelectedProject.ProjectID != null)
            {
                // stop listening to previous project 
                if (_currentListeningProjectID != SelectedProject.ProjectID)
                {
                    FirebaseHelper.StopListeningToTasks();
                    _currentListeningProjectID = SelectedProject.ProjectID;
                }
                var tasks = await FirebaseHelper.GetTasks(SelectedProject.ProjectID);
                UpdateTaskCollections(tasks);
                var is_updating = false;
                // Start listening for real-time updates
                FirebaseHelper.ListenToTaskChanges(SelectedProject.ProjectID, async tasks => 
                {
                    var u_task = tasks.FirstOrDefault();
                    if (u_task != null && u_task.isNOtified == false && !(is_updating))
                    {
                        is_updating = true;
                        ShowTaskNotification.Show(u_task);
                        u_task.isNOtified = true;
                        await FirebaseHelper.UpdateTask(u_task);
                        is_updating = false;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateTaskCollections(tasks);
                    });
                });
            }
        }

        public ISeries[] TaskStatusSeries { get; set; }
        public string[] TaskStatusLabels { get; set; }

        private void UpdateTaskCollections(List<TaskItem> tasks)
        {
            SelectedProject.Tasks = tasks;
            ToDoTasks = [.. tasks.Where(t => t.Status == Models.TaskStatus.ToDo)];
            InProgressTasks = [.. tasks.Where(t => t.Status == Models.TaskStatus.InProgress)];
            InReviewTasks = [.. tasks.Where(t => t.Status == Models.TaskStatus.InReview)];
            CompletedTasks = [.. tasks.Where(t => t.Status == Models.TaskStatus.Completed)];

            TaskStatusSeries =
             [
                new PieSeries<int> { Values = [ToDoTasks.Count], Name = "To-Do", Fill = new SolidColorPaint(SKColors.LightBlue) },
                    new PieSeries<int> { Values = [InProgressTasks.Count], Name = "In Progress", Fill = new SolidColorPaint(SKColors.Orange) },
                    new PieSeries<int> { Values = [InReviewTasks.Count], Name = "In Review", Fill = new SolidColorPaint(SKColors.MediumPurple) },
                    new PieSeries<int> { Values = [CompletedTasks.Count], Name = "Completed", Fill = new SolidColorPaint(SKColors.LightGreen) },
                ];
            TaskStatusLabels = ["To-Do", "In Progress", "In Review", "Completed"];

            OnPropertyChanged(nameof(TaskStatusSeries));
            OnPropertyChanged(nameof(TaskStatusLabels));
        }

        public async Task<bool> UpdateTaskStatusAsync(TaskItem task, PMTaskStatus newStatus)
        {
            if (task == null) return false;
            if (CurrentUser.NodeID != task.CreatedById)
                return false;
            if (task.Status == newStatus) return true;

            var oldStatus = task.Status;

            RemoveFromCollectionByStatus(task, oldStatus);
            task.Status = newStatus;
            AddToCollectionByStatus(task, newStatus);

            try
            {
                var ok = await FirebaseHelper.UpdateTask(task);
                if (!ok)
                {
                    RemoveFromCollectionByStatus(task, newStatus);
                    task.Status = oldStatus;
                    AddToCollectionByStatus(task, oldStatus);
                }
                return ok;
            }
            catch(Exception ex)
            {
                RemoveFromCollectionByStatus(task, newStatus);
                task.Status = oldStatus;
                AddToCollectionByStatus(task, oldStatus);

                System.Diagnostics.Debug.WriteLine($"UpdateTaskStatusAsync failed: {ex}");
                return false;
            }
        }

        private void RemoveFromCollectionByStatus(TaskItem task, PMTaskStatus status)
        {
            switch (status)
            {
                case PMTaskStatus.ToDo:
                    if (ToDoTasks.Contains(task)) ToDoTasks.Remove(task);
                    break;
                case PMTaskStatus.InProgress:
                    if (InProgressTasks.Contains(task)) InProgressTasks.Remove(task);
                    break;
                case PMTaskStatus.InReview:
                    if (InReviewTasks.Contains(task)) InReviewTasks.Remove(task);
                    break;
                case PMTaskStatus.Completed:
                    if (CompletedTasks.Contains(task)) InReviewTasks.Remove(task);
                    break;
            }
        }

        private void AddToCollectionByStatus(TaskItem task, PMTaskStatus status)
        {
            switch (status)
            {
                case PMTaskStatus.ToDo:
                    if (!ToDoTasks.Contains(task)) ToDoTasks.Add(task);
                    break;
                case PMTaskStatus.InProgress:
                    if (!InProgressTasks.Contains(task)) InProgressTasks.Add(task);
                    break;
                case PMTaskStatus.InReview:
                    if (!InReviewTasks.Contains(task)) InReviewTasks.Add(task);
                    break;
                case PMTaskStatus.Completed:
                    if (!CompletedTasks.Contains(task)) CompletedTasks.Add(task);
                    break;
            }
        }

        public void Cleanup()
        {
            FirebaseHelper.StopListeningToTasks();
        }

        public ISeries[] PrioritySeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        private async Task LoadPriorityChartData()
        {
            var priorities = Enum.GetValues(typeof(PriorityLevel)).Cast<PriorityLevel>().ToList();
            var counts = priorities.Select(p => SelectedProject.Tasks.Where(t => t.ProjectID == SelectedProject.ProjectID).Count(t => (int)t.Priority == (int)p)).ToList();
            PrioritySeries =
            [
                new ColumnSeries<int>
                {
                    Values = counts,
                    Name = "Tasks by Priority"
                }
            ];

            XAxes =
            [
                new Axis
                {
                    Labels = priorities.Select(p => p.ToString()).ToArray(),
                    LabelsRotation = 15
                }
            ];

            YAxes =
            [
                new Axis
                {
                    Name = "count",
                    MinLimit = 0
                }
            ];

            OnPropertyChanged(nameof(PrioritySeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));

        }

    }
}
