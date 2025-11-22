using Core.Business.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NumberArtistView.Services.Models;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberArtistView.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "NumberArtist.db3");

            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Core.Business.Objects.DxfFileEntry>().Wait();

            // Ensure a unique index to avoid duplicate resource entries per user.
            // This enforces uniqueness at the DB level and prevents race/duplicate insert issues.
            _database.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS idx_dxffile_res_user ON DxfFileEntry(ResourceName, AppUserId);").Wait();
        }

        public async Task InitializeAsync()
        {
            // Use await instead of .Wait()
            await CopyFilesFromServerToLocalDb();
        }

        public async Task CopyFilesFromServerToLocalDb()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://numberartist.officeblox.co.uk:5015/");

            var token = Preferences.Get("auth_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var userIdString = await SecureStorage.GetAsync("userId");
            if (string.IsNullOrEmpty(userIdString))
                throw new InvalidOperationException("User ID not found in secure storage.");
            Guid userId = Guid.Parse(userIdString);

            var response = await httpClient.GetAsync("api/dxffiles");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var entities = JsonConvert.DeserializeObject<List<Root>>(json);

                foreach (var fileFromServer in entities)
                {
                    try
                    {
                            // Normalize the resource name (stable unique identifier)
                        var resourceName = fileFromServer.storedFileName != null ? fileFromServer.storedFileName.Trim() : string.Empty;

                        // Check existence by ResourceName + AppUserId (stable key)
                        var existingFile = await _database.Table<DxfFileEntry>()
                            .Where(f => f.ResourceName == resourceName && f.AppUserId == userId)
                            .FirstOrDefaultAsync();

                        if (existingFile == null)
                        {
                            var newFileEntry = new DxfFileEntry
                            {
                                Name = fileFromServer.fileName != null ? fileFromServer.fileName.Trim() : string.Empty,
                                ResourceName = resourceName,
                                AppUserId = userId
                            };

                            try
                            {
                                await _database.InsertAsync(newFileEntry);
                            }
                            catch (SQLiteException)
                            {
                                // If another thread/process inserted the same resource concurrently
                                // the unique index will raise a constraint error; ignore it.
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file: {ex.Message}");
                    }
                }
            }
            return;
        }

        public Task<int> GetRecordCount()
        {
            return _database.Table<DxfFileEntry>().CountAsync();
        }

        public async Task<List<DxfFileEntry>> GetDxfFilesAsync(Guid userId)
        {
            return await _database.Table<DxfFileEntry>().Where(f => f.AppUserId == userId).ToListAsync();
        }

        public async Task<DxfFileEntry?> GetDxfFileByNameAsync(string resourceName)
        {
            return await _database.Table<DxfFileEntry>()
                .Where(f => f.ResourceName == resourceName)
                .FirstOrDefaultAsync();
        }

        public async Task<DxfFileEntry?> GetDxfFileBytesAsync(string resourceName)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://numberartist.officeblox.co.uk:5015/");

            var token = Preferences.Get("auth_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var userIdString = await SecureStorage.GetAsync("userId");
            if (string.IsNullOrEmpty(userIdString))
                throw new InvalidOperationException("User ID not found in secure storage.");
            Guid userId = Guid.Parse(userIdString);

            var response = await httpClient.GetAsync($"api/dxffiles/GetResource/{resourceName}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var entity = JsonConvert.DeserializeObject<Root>(json);

                if (entity != null)
                {
                    MemoryStream ms = new MemoryStream();
                    using (BsonDataWriter writer = new BsonDataWriter(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(writer, entity);
                    }

                    var outentity = ms.ToArray();
                    if (outentity != null)
                    {
                        File.WriteAllBytes(resourceName, outentity);
                    }

                    var fileEntry = new DxfFileEntry
                    {
                        Id = entity.id,
                        Name = entity.fileName,
                        ResourceName = entity.storedFileName,
                        AppUserId = userId
                    };
                    return fileEntry;
                }
                return null;
            }
            return null;
        }
    }
}