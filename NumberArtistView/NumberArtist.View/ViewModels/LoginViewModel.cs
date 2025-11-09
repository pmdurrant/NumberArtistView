using Core.Business.Objects;
using NumberArtistView;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace NumberArtist.View.ViewModels
{
    public partial class LoginViewModel : INotifyPropertyChanged
    {
        private string _username ;
        private string _password  ;
        private readonly HttpClient _httpClient;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            _httpClient = new HttpClient();
            // The base address must be changed to your API's address.
            // Use http://10.0.2.2:<port> for Android emulators.
            _httpClient.BaseAddress = new Uri("https://numberartist.officeblox.co.uk:5015/"); 
            LoginCommand = new Command(async () => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            var loginModel = new { Username, Password };
            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Store the token securely
                    await SecureStorage.SetAsync("auth_token", tokenResponse.Token);
                    Preferences.Set("auth_token", tokenResponse.Token);
                    Preferences.Set("userId", tokenResponse.UserId);
                    await SecureStorage.SetAsync("userId", tokenResponse.UserId);
                    // Navigate to the main page on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage = new AppShell();
                    });
                }
                else
                {
                    // Handle unsuccessful login
                    await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., network errors
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}