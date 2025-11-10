using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManagerPro_SOLID.DirectoryUtilities;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectManagerPro_SOLID.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private UserRole selectedRole = UserRole.None;

        [ObservableProperty]
        private string usernameError;

        [ObservableProperty]
        private string passwordError;

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private Visibility progressEnable = Visibility.Collapsed;

        [ObservableProperty]
        private string roleError;

        public ObservableCollection<UserRole> Roles { get; } = new(Enum.GetValues(typeof(UserRole))
                   .Cast<UserRole>()
                   .ToList());

        public RegisterViewModel() { }

        public RegisterViewModel(User user) : this()
        {
            Username = user?.Username ?? string.Empty;
            Password = user?.Password ?? string.Empty;
            RememberMe = user != null;
        }

        [RelayCommand]
        private async void Register()
        {
            try
            {
                UsernameError = PasswordError = RoleError = string.Empty;
                bool isValid = true;

                if (string.IsNullOrWhiteSpace(Username))
                {
                    UsernameError = "Username is required.";
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    PasswordError = "Password is required.";
                    isValid = false;
                }

                if (SelectedRole == UserRole.None)
                {
                    RoleError = "Please select a role.";
                    isValid = false;
                }

                if (!isValid)
                    return;
                ProgressEnable = Visibility.Visible;

                var hashedPassword = PasswordHelper.HashPassword(Password);

                var RegisterUser = new User { NodeID = Guid.NewGuid().ToString(), Username = Username, Password = hashedPassword, Role = selectedRole };

                var UserToSave = RegisterUser.DeepClone();

                UserToSave.Password = Password;

                var success = await FirebaseHelper.RegisterUser(RegisterUser);
                if (success)
                {
                    if (RememberMe)
                        DirHelper.SaveInfo(UserToSave);
                    // Application.Current.MainWindow.Content = new MainView(RegisterUser);
                }
                else
                {
                    Application.Current.MainWindow.Content = new LoginView(RegisterUser);
                }
            }
            finally
            {
                ProgressEnable = Visibility.Hidden;
            }
        }

        [RelayCommand]
        private void GoToLogin()
        {
            Application.Current.MainWindow.Content = new LoginView(new User());
        }
    }
}
