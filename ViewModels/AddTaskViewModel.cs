using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectManagerPro_SOLID.ViewModels
{
    public partial class AddTaskViewModel : ObservableObject
    {
        public AddTaskViewModel() { }

        [ObservableProperty]
        private string title = string.Empty;
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

        private User _currentUser;
        private Project _project;
        private Window _tw;

        public AddTaskViewModel(User currentUser, Project project, Window tw) : this()
        {
            _currentUser = currentUser;
            _project = project;
            _tw = tw;
            LoadDevelopers();
        }

        private async void LoadDevelopers()
        {
            var allUsers = await FirebaseHelper.GetUsers(Enums.UserRole.Developer);
            Developers = new ObservableCollection<User>(allUsers);
        }

        [RelayCommand]
        private async Task AddTask()
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(Title))
            {
                TitleError = "Please Enter Title";
                isValid = false;
            }
            else
            {
                TitleError = "";
            }

            if (string.IsNullOrWhiteSpace(SelectedAssignedTo?.Username))
            {
                AssignedToError = "Please Select Person";
                isValid = false;
            }
            else
            {
                AssignedToError = "";
            }

            if (string.IsNullOrWhiteSpace(SelectedPriority))
            {
                PriorityError = "Please Select Priority";
                isValid = false;
            }
            else
            {
                PriorityError = "";
            }

            if (!(StartDate.HasValue))
            {
                StartDateError = "Please Enter Start Date";
                isValid = false;
            }
            else
            {
                StartDateError = "";
            }

            if (!DueDate.HasValue)
            {
                DueDateError = "Please Enter Due Date";
                isValid = false;
            }
            else if (DueDate < StartDate)
            {
                DueDateError = "Due Date cannot be before the start date";
                isValid = false;
            }
            else
            {
                DueDateError = "";
            }

            if (!isValid)
                return;

            var priorityString = (SelectedPriority ?? string.Empty).Replace(" ", "", StringComparison.OrdinalIgnoreCase);
            Enum.TryParse<PriorityLevel>(priorityString, true, out var p);

            var TaskItem = new TaskItem
            {
                Title = Title,
                AssignedTo = SelectedAssignedTo,
                isNOtified = false,
                Priority = p,
                StartDate = StartDate ?? DateTime.Today,
                DueDate = DueDate ?? DateTime.Today.AddDays(7),
                ModifiedDateTime = DateTime.UtcNow,
                Description = Description ?? string.Empty,
                Status = Models.TaskStatus.ToDo,
                ProjectID = _project.ProjectID,
                CreatedById = _currentUser.NodeID,
                CreatedByUsername = _currentUser.Username,
                TaskID = Guid.NewGuid().ToString(),
            };

            await FirebaseHelper.AddTask(TaskItem);

            // ShowTaskNotification.Show(TaskItem);

            _tw.Close();
        }
    }
}