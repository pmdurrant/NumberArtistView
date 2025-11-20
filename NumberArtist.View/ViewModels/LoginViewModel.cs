csharp NumberArtistView\NumberArtist.View\ViewModels\LoginViewModel.cs
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace NumberArtist.View.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private class TokenResponse
        {
            public string Token { get; set; }
            public string UserId { get; set; } = string.Empty;
        }

        private string _ipAddress = string.Empty;
        private string _username;
        private string _password;
        private readonly HttpClient _httpClient;

        public event PropertyChangedEventHandler PropertyChanged;

        public string IpAddress
        {
            get => _ipAddress;
            private set
            {
                _ipAddress = value;
                OnPropertyChanged();
            }
        }

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
            // Use a safe default base address; avoid using IpAddress while it's uninitialized.
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://numberartist.officeblox.co.uk")
            };

            LoginCommand = new Command(async () => await LoginAsync());
        }

        // Call this from the page lifecycle (OnAppearing) to perform optional DNS resolution.
        public async Task InitializeAsync(string urlOrHost = "https://numberartist.officeblox.co.uk")
        {
            try
            {
                string host = urlOrHost;
                string scheme = "https";

                if (Uri.TryCreate(urlOrHost, UriKind.Absolute, out var uri))
                {
                    host = uri.Host;
                    scheme = uri.Scheme;
                }

                var addresses = await Dns.GetHostAddressesAsync(host);
                var ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();

                if (ip != null)
                {
                    IpAddress = ip.ToString();

                    // Try to update HttpClient base address to the resolved IP (preserve scheme)
                    if (Uri.TryCreate($"{scheme}://{IpAddress}", UriKind.Absolute, out var baseUri))
                    {
                        _httpClient.BaseAddress = baseUri;
                    }
                }
                else
                {
                    IpAddress = "Not found";
                }
            }
            catch (Exception ex)
            {
                IpAddress = $"Error: {ex.Message}";
                // Do not rethrow; keep initialize safe for XAML construction.
            }
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

                    if (tokenResponse?.Token != null)
                    {
                        await SecureStorage.SetAsync("auth_token", tokenResponse.Token);
                        Preferences.Set("auth_token", tokenResponse.Token);
                        Preferences.Set("userId", tokenResponse.UserId);
                        await SecureStorage.SetAsync("userId", tokenResponse.UserId);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Application.Current.MainPage = new AppShell();
                        });
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}