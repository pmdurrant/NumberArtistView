using Core.Business.Objects;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NumberArtistView.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseAddress;

        public AuthService()
        {
            _baseAddress = ApiConfiguration.GetApiUrl();
            Debug.WriteLine($"AuthService: Using API URL: {_baseAddress}");

#if DEBUG
            // This handler is necessary for development on Android/iOS
            // to bypass SSL certificate validation for the self-signed cert.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    Debug.WriteLine($"AuthService: SSL Certificate validation: {errors}");
                    return true;
                }
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new System.Uri(_baseAddress),
                Timeout = TimeSpan.FromSeconds(30)
            };
#else
            // In production, you would use a valid certificate.
            _httpClient = new HttpClient()
            {
                BaseAddress = new System.Uri(_baseAddress),
                Timeout = TimeSpan.FromSeconds(30)
            };
#endif

            // Set Host header when using IP addresses for SNI
            if (ApiConfiguration.UseFallbackIP || ApiConfiguration.UseLocalhost)
            {
                _httpClient.DefaultRequestHeaders.Host = ApiConfiguration.GetHostname() + ":5015";
                Debug.WriteLine($"AuthService: Set Host header to: {_httpClient.DefaultRequestHeaders.Host}");
            }
        }

        public async Task<AuthResponse> LoginAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"AuthService: Attempting login for user: {username}");
                Debug.WriteLine($"AuthService: API Base Address: {_baseAddress}");

                var request = new LoginModel { Username = username, Password = password };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                Debug.WriteLine($"AuthService: Posting to /api/auth/login");
                var response = await _httpClient.PostAsync("/api/auth/login", content);

                Debug.WriteLine($"AuthService: Response Status Code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    Debug.WriteLine($"AuthService: Login successful, token received");
                    return authResponse;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"AuthService: Login failed - Status: {response.StatusCode}, Error: {errorContent}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"AuthService: HTTP Request Exception: {ex.Message}");
                Debug.WriteLine($"AuthService: Inner Exception: {ex.InnerException?.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"AuthService: Request timeout: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuthService: Unexpected error: {ex.Message}");
                throw;
            }
        }
    }
}