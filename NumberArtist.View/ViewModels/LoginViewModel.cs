using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using NumberArtist.View;
using NumberArtistView.Services;

namespace NumberArtist.View.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _username;
        private string _password;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new Command(async () => await OnLogin());
        }

        private async Task OnLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Username and password are required.", "OK");
                return;
            }

            var authResponse = await _authService.LoginAsync(Username, Password);

            if (authResponse == null)
            {
                await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
                return;
            }

            // Store user details and token securely
            await SecureStorage.SetAsync("auth_token", authResponse.Token);
            Preferences.Set("UserId", authResponse.UserId);
            Preferences.Set("Username", authResponse.Username);

            // Set the main page of the application to be the AppShell
            Application.Current.MainPage = new AppShell();

            // Navigate to the MainPage
            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}