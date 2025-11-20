using Core.Business.Objects;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberArtistView.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        // The base address of your API.
        // For Android emulator, use 10.0.2.2 to connect to localhost on the host machine.
        // For Windows, you can use https://localhost:5015
        private readonly string _baseAddress = "https://numberartist.officeblox.co.uk:5015";

        public AuthService()
        {
#if DEBUG
            // This handler is necessary for development on Android/iOS
            // to bypass SSL certificate validation for the self-signed cert.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler) { BaseAddress = new System.Uri(_baseAddress) };
#else
            // In production, you would use a valid certificate.
            _httpClient = new HttpClient() { BaseAddress = new System.Uri(_baseAddress) };
#endif
        }

        public async Task<AuthResponse> LoginAsync(string email, string password)
        {
            var request = new AuthRequest { Email = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }

            return null;
        }
    }
}