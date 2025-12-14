// C# HttpClient example for UploadPair
using System.Net.Http;
using System.Net.Http.Headers;

public async Task<HttpResponseMessage> UploadPairAsync(Stream dxfStream, string dxfName, Stream jpgStream, string jpgName, string token)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StreamContent(dxfStream), "file1", dxfName);
    form.Add(new StreamContent(jpgStream), "file2", jpgName);

    var req = new HttpRequestMessage(HttpMethod.Post, "/api/DxfFiles/UploadPair") { Content = form };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    return await _httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead);
}