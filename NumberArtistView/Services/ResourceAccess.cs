using Core.Business.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NumberArtistView.Services
{
    public class ResourceAccess
    {
        private readonly HttpClient _httpClient;
    
        public ResourceAccess()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://numberartist.officeblox.co.uk:5015/")
            };

            var token = Preferences.Get("auth_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<string> GetResourceAsync(string resourceName)
        {
            // 1. Define the target directory and file path within the app's data folder.
            var targetDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "dxf_files");
            var targetFile = Path.Combine(targetDirectory, resourceName);

            // 2. If the file already exists, return its content.
            if (File.Exists(targetFile))
            {
                return await File.ReadAllTextAsync(targetFile);
            }

            try
            {
                // 3. Create the directory if it doesn't exist.
                Directory.CreateDirectory(targetDirectory);

                // 4. Make the API call to get the resource.
                var response = await _httpClient.GetAsync($"api/dxffiles/GetResource?resourceName={resourceName}");
                response.EnsureSuccessStatusCode(); // Throws an exception for non-2xx responses.

                // 5. Read the content and save it to the file.
                var content = await response.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(targetFile, content);

                // 6. Return the content.
                return content;
            }
            catch (HttpRequestException ex)
            {
                // Handle potential network errors or bad responses.
                // Consider logging the error for debugging.
                Console.WriteLine($"Error fetching resource: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Handle other potential errors (e.g., file system access).
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return null;
            }
        }
    }
}