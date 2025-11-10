using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectManagerPro_SOLID.DirectoryUtilities;
using ProjectManagerPro_SOLID.Helpers;
using ProjectManagerPro_SOLID.Models;
using ProjectManagerPro_SOLID.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ProjectManagerPro_SOLID.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string username;
        [ObservableProperty]
        public Visibility progressEnable = Visibility.Collapsed;
        [ObservableProperty]
        private string password;
        [ObservableProperty]
        private bool rememberMe;
        [ObservableProperty]
        private string validationError;

        public LoginViewModel() { }

        public LoginViewModel(User user) : this()
        {
            Username = user?.Username ?? string.Empty;
            Password = user?.Password ?? string.Empty;
            RememberMe = user != null;
            if (RememberMe)
                LoginCommand.Execute(user);
        }

        [RelayCommand]
        private async void Login()
        {
            try
            {
                ValidationError = string.Empty;
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ValidationError = "Plese Enter Username and Password";
                    return;
                }
                ProgressEnable = Visibility.Visible;
                var user = await FirebaseHelper.CheckUser(Username);
                var IsValidPass = PasswordHelper.VerifyPassword(user?.Password, Password);
                if (user != null && IsValidPass)
                {
                    if (RememberMe)
                    {
                        user.Password = Password;
                        DirHelper.SaveInfo(user);
                    }
                    var mainView = new MainView(user);
                    Application.Current.MainWindow.Content = mainView;
                }
                else if (user != null && (user.Username != Username || !IsValidPass))
                {
                    ValidationError = "Invalid Creadential";
                }
                else
                {
                    Application.Current.MainWindow.Content = new RegisterView(new Models.User { Username = Username, Password = Password });
                }
            }
            finally
            {
                ProgressEnable = Visibility.Collapsed;
            }
        }

        [RelayCommand]
        private void GoToRegister()
        {
            Application.Current.MainWindow.Content = new RegisterView(new Models.User());
            }
    }
}
