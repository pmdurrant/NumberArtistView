using Core.Business.Objects;
using NumberArtistView;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NumberArtist.View.ViewModels
{
    public partial class LoginViewModel : INotifyPropertyChanged
    {
        private string _username ;
        private string _password  ;
        private readonly HttpClient _httpClient;
        private bool _resolveStarted;

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

        // Plan (pseudocode):
        // 1. Create an HttpClientHandler that disables proxy usage. (UseProxy = false)
        //    - This prevents system or emulator proxies from redirecting requests to localhost (127.0.0.1:80).
        // 2. Create HttpClient using that handler, and set BaseAddress to the desired https URI (include trailing slash).
        // 3. Wire up LoginCommand as before.
        // 4. Do NOT overwrite BaseAddress with any resolved IP (avoid ResolveIpFromUrlAsync changing base to an IP that maps to localhost).
        //    - If you must resolve the host to an IP, keep the original host in the request Host header and SNI; do not replace BaseAddress with plain IP:port.
        // 5. Keep the rest of LoginAsync flow unchanged so PostAsync("api/auth/login") combines with BaseAddress.
        //
        // This ensures requests use the configured BaseAddress and avoids emulator/proxy rewriting to 127.0.0.1:80.

        public LoginViewModel()
        {
            // Disable proxy to avoid requests being routed to a local proxy (which can result in 127.0.0.1:80)
            var handler = new HttpClientHandler
            {
                UseProxy = false
            };

            _httpClient = new HttpClient(handler)
            {
                // Ensure trailing slash to avoid URI composition surprises
                BaseAddress = new Uri("https://numberartist.officeblox.co.uk:5015/")
            };

            LoginCommand = new Command(async () => await LoginAsync());

            // Start the resolver in background (non-blocking).
            // Use StartResolve to ensure it only starts once.
            StartResolve("https://numberartist.officeblox.co.uk:5015");
        }

        private void StartResolve(string url)
        {
            if (_resolveStarted) return;
            _resolveStarted = true;
            // Fire-and-forget on thread pool; exceptions are handled inside ResolveIpFromUrlAsync.
            _ = Task.Run(() => ResolveIpFromUrlAsync(url));
        }

        private async Task ResolveIpFromUrlAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return;

                var uri = new Uri(url);
                var host = uri.Host;

                // Perform DNS resolution
                var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                // Optionally process addresses (e.g., pick IPv4), but DO NOT overwrite _httpClient.BaseAddress.
                // This method exists primarily to trigger DNS resolution / logging and avoid emulator proxy issues.
                foreach (var addr in addresses)
                {
                    Debug.WriteLine($"Resolved {host} -> {addr}");
                }
            }
            catch (Exception ex)
            {
                // Log and swallow exceptions so background work doesn't crash the app.
                Debug.WriteLine($"ResolveIpFromUrlAsync error: {ex.Message}");
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
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message} ipaddress {IpAddress}", "OK");
            }
        }

        // Add this public async method to LoginViewModel to fix CS1061
        public async Task InitializeAsync()
        {
            // Place any initialization logic here.
            // Trigger resolver from InitializeAsync as well (won't start twice).
            StartResolve("https://numberartist.officeblox.co.uk:5015");
            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}