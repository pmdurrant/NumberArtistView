using Core.Business.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
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

            if (!File.Exists(dbPath))
            {
                _database = new SQLiteAsyncConnection(dbPath);
                _database.CreateTableAsync<Core.Business.Objects.DxfFileEntry>().Wait();
            }
            else
            { 
                _database = new SQLiteAsyncConnection(dbPath);
            }
            //_database.DropTableAsync<DxfFileEntry>().Wait();
            //_database.CreateTableAsync<Core.Business.Objects.DxfFileEntry>().Wait();
        }

        public async Task InitializeAsync()
        {




            CopyFilesFromServerToLocalDb().Wait();

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
            
            Guid userId = Guid.Parse(await SecureStorage.GetAsync("userId"));
            
            var response = await httpClient.GetAsync("api/dxffiles");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                // This 'entities' list contains DxfFile objects from the server
               // var entities = JsonConvert.DeserializeObject<List<DxfFileEntry>>(json);
                var entities = JsonConvert.DeserializeObject<List<Root>>(json);
                
                foreach (var fileFromServer in entities)
                {
                    try
                    {
                        // Query the DxfFileEntry table, not DxfFile
                        var existingFile = await _database.Table<DxfFileEntry>()
                            // Check if a file with the same name and user ID already exists
                            .Where(f => f.Name == fileFromServer.fileName && f.AppUserId == userId)
                            .FirstOrDefaultAsync(); 

                        // If no matching file is found in the local DB, insert it.
                        if (existingFile == null)
                        {
                            // Map the server object (DxfFile) to a database entity (DxfFileEntry)
                            var newFileEntry = new DxfFileEntry
                            {
                               
                                Name = fileFromServer.fileName,
                                ResourceName = fileFromServer.storedFileName, // Or whatever property holds the resource name
                                AppUserId = userId 
                            };
                            await _database.InsertAsync(newFileEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        // It's good practice to log the exception for debugging
                        Console.WriteLine($"Error processing file: {ex.Message}");
                    }
                }
            }
            return;
        }
        public class Root
        {
            public int id { get; set; }
            public string fileName { get; set; }
            public string contentType { get; set; }
            public string storedFileName { get; set; }
            public DateTime uploadedAt { get; set; }
            public string appUserId { get; set; }
            public object appUser { get; set; }
        }

        public Task<int> GetRecordCount()
        {
            return _database.Table<DxfFileEntry>().CountAsync();
        }
        public async Task<List<DxfFileEntry>> GetDxfFilesAsync(Guid userId)
        {

            // var fred = await _database.Table<DxfFileEntry>().ToListAsync();

            // Console.WriteLine(fred.Count);
            return await _database.Table<DxfFileEntry>().Where(f => f.AppUserId == userId).ToListAsync();
            // return fred;
        }

        public async Task<DxfFileEntry?> GetDxfFileByNameAsync(string resourceName)
        {
          //  await InitializeAsync();
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

            Guid userId = Guid.Parse(await SecureStorage.GetAsync("userId"));

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

                    // Map Root to DxfFileEntry before returning
                    var fileEntry = new DxfFileEntry
                    {
                        Id = entity.id,
                        Name = entity.fileName,
                        ResourceName = entity.storedFileName,
                        AppUserId = userId
                    };
                    return fileEntry;
                }
                // Explicitly return null if entity is null
                return null;
            }
            return null;
        }


    }
    
}