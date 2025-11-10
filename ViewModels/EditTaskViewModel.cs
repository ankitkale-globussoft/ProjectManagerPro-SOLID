using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using System.Windows;
using ProjectManagerPro_SOLID.Views;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Xaml.Behaviors.Media;

namespace ProjectManagerPro_SOLID.ViewModels
{
    public partial class EditTaskViewModel : ObservableObject
    {
        private TaskItem _task;

        public EditTaskViewModel()
        {
            LoadDevelopers();
        }

        [ObservableProperty]
        private string title;
        [ObservableProperty]
        private string? titleError;

        [ObservableProperty]
        private ObservableCollection<User> developers = new();

        [ObservableProperty]
        private User? selectedAssignedTo;
        [ObservableProperty]
        private string? assignedToError;

        [ObservableProperty]
        private string? selectedPriority;
        [ObservableProperty]
        private string? priorityError;
     
        [ObservableProperty]
        private DateTime? startDate;
        [ObservableProperty]
        private string? startDateError;

        [ObservableProperty]
        private DateTime? dueDate;
        [ObservableProperty]
        private string? dueDateError;

        [ObservableProperty]
        private string description = string.Empty;
        [ObservableProperty]
        private string? descriptionError;

        [ObservableProperty]
        private ObservableCollection<Comment> comments = new();

        [ObservableProperty]
        private string? commentText;

        [ObservableProperty]
        private bool isReadOnly;

        [ObservableProperty]
        private bool canEdit;

        [ObservableProperty]
        private Visibility editButtonsVisibility;

        private Window _et;
        private bool _isCreator;
        private string _currentUser;

        public EditTaskViewModel(TaskItem task, Window et, User usr) : this()
        {
            _task = task;
            _et = et;
            _currentUser = usr.Username;
            
            _isCreator = task.CreatedById == usr.NodeID;

            IsReadOnly = !_isCreator;
            CanEdit = _isCreator;
            EditButtonsVisibility = _isCreator ? Visibility.Visible : Visibility.Collapsed;

            Title = _task.Title;
            SelectedPriority = _task.Priority.ToString();
            StartDate = _task.StartDate;
            DueDate = _task.DueDate;
            Description = _task.Description;

            StartListeningToComments(_task.TaskID);
        }

        private async void LoadDevelopers()
        {
            var allUsers = await FirebaseHelper.GetUsers(Enums.UserRole.Developer);
            Developers = new ObservableCollection<User>(allUsers);
            SelectedAssignedTo = Developers.FirstOrDefault(d => d.Username == _task.AssignedTo.Username);
        }

        [RelayCommand]
        private async Task SaveChanges(TaskItem task)
        {
            if (_task == null) return;
            var priorityString = (SelectedPriority ?? string.Empty).Replace(" ", "", StringComparison.OrdinalIgnoreCase);

            _task.Title = Title;
            _task.isNOtified = false;
            _task.AssignedTo = SelectedAssignedTo ?? _task.AssignedTo;
            if (Enum.TryParse<PriorityLevel>(priorityString, true, out var p))
                _task.Priority = p;
            if (StartDate.HasValue) _task.StartDate = StartDate.Value;
            if (DueDate.HasValue) _task.DueDate = DueDate.Value;
            _task.Description = Description;
            _task.ModifiedDateTime = DateTime.UtcNow;

            await FirebaseHelper.UpdateTask(_task);
            _et.Close();
        }

        public void StartListeningToComments(string taskItemId)
        {
            FirebaseHelper.ListenToComments(taskItemId, commentsList =>
            {
                // Make sure UI updates happen on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Comments.Clear();
                    foreach (var comment in commentsList.OrderByDescending(c => c.Timestamp))
                    {
                        Comments.Add(comment);
                    }

                    // Auto-scroll to bottom to show latest comments
                });
            });
        }

        [RelayCommand]
        private async Task AddComment()
        {
            if (String.IsNullOrWhiteSpace(CommentText)){
                return;
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Message = CommentText,
                TaskItemId = _task.TaskID,
                UserName = _currentUser,
                Timestamp = DateTime.Now
            };

            var ok = await FirebaseHelper.AddTaskComment(comment);
            if (ok)
                CommentText = String.Empty;
        }

        [RelayCommand]
        private async Task DeleteTask()
        {
            if (_task == null) return;
            await FirebaseHelper.DeleteTask(_task);
            _et.Close();
        }
    }
}
