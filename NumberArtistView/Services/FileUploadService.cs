using Core.Business.Objects.Models.Dto;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NumberArtistView.Services
{
 

    public class FileUploadService
    {
        private readonly HttpClient _httpClient;
        public FileUploadService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<int> UploadDxfAsync(Stream dxfStream, string fileName, string token, string contentType = "application/octet-stream")
        {
            // client-side validation
            if (!fileName.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected .dxf file", nameof(fileName));

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(dxfStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/DxfFiles") { Content = content };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<DxfFileDto>();
            if (created == null) throw new InvalidOperationException("DXF upload returned no body");
            return created.Id;
        }

        public async Task<DxfFileDto> UploadBackgroundAsync(Stream jpgStream, string fileName, int dxfId, string token, string contentType = "image/jpeg")
        {
            if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected .jpg file", nameof(fileName));

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(jpgStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            // Pass the dxfId so server can link the background to the dxf entity
            content.Add(new StringContent(dxfId.ToString()), "dxfId");

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/DxfFiles") { Content = content };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<DxfFileDto>();
            return created!;
        }
    }
}
