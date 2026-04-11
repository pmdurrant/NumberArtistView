using Core.Business.Objects;
using Core.Business.Objects.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NumberArtistView.Models;
using NumberArtistView.Services.Models;
using SQLite;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberArtistView.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private string _filesPathRef;
        private string dbPath;

        public DatabaseService()
        {
            try
            {
                Debug.WriteLine("DatabaseService: constructor start");
                dbPath = Constants.DatabasePath;
                Debug.WriteLine($"DatabaseService: DB path = {Constants.DatabasePath}");

                _database = new SQLiteAsyncConnection(dbPath, Constants.Flags);
                _database.CreateTableAsync<Core.Business.Objects.DxfFileEntry>().Wait();
                Debug.WriteLine("DatabaseService: Ensured DxfFileEntry table exists");

                // Ensure ReferenceDrawing table exists
                _database.CreateTableAsync<ReferenceDrawing>().Wait();
                
                // Ensure LayerGroupState table exists
                _database.CreateTableAsync<LayerGroupState>().Wait();
                Debug.WriteLine("DatabaseService: Ensured LayerGroupState table exists");
                
                // Ensure PolylineState table exists
                _database.CreateTableAsync<PolylineState>().Wait();
                Debug.WriteLine("DatabaseService: Ensured PolylineState table exists");
                
                _filesPathRef = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files");
                Directory.CreateDirectory(_filesPathRef);

                

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DatabaseService: constructor error: {ex}");
                Console.WriteLine($"DatabaseService: constructor error: {ex}");
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            Debug.WriteLine("DatabaseService: InitializeAsync called");
            // Use await instead of .Wait()
            await CopyFilesFromServerToLocalDb();
            Debug.WriteLine("DatabaseService: InitializeAsync completed");
        }

        public async Task<ReferenceDrawing> GetDxfFileBackgroundIdAsyncByDxfFileId(long dxfFileId)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFileBackgroundIdAsyncByDxfFileId dxfFileId={dxfFileId}");
            var result = await _database.Table<ReferenceDrawing>()
                .Where(f => f.DxfFileId == dxfFileId)
                .FirstOrDefaultAsync();
            Debug.WriteLine($"DatabaseService: Found ReferenceDrawing={(result != null ? result.Name : "<null>")}");
            return result;
        }
        public async Task CopyFilesFromServerToLocalDb()
        {
            Debug.WriteLine("DatabaseService: CopyFilesFromServerToLocalDb start");
            try
            {
                // Use ApiConfiguration to get the correct URL (handles localhost vs production)
                var apiUrl = ApiConfiguration.GetApiUrl();
                Debug.WriteLine($"DatabaseService: Using API URL: {apiUrl}");

#if DEBUG
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        Debug.WriteLine($"DatabaseService: SSL Certificate validation: {errors}");
                        return true; // Accept all certificates in debug mode
                    }
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/"),
                    Timeout = TimeSpan.FromSeconds(30)
                };
#else
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/"),
                    Timeout = TimeSpan.FromSeconds(30)
                };
#endif

                // When using IP address, set the Host header for proper routing
                if (ApiConfiguration.UseFallbackIP || ApiConfiguration.UseLocalhost)
                {
                    httpClient.DefaultRequestHeaders.Host = ApiConfiguration.GetHostname() + ":5015";
                    Debug.WriteLine($"DatabaseService: Set Host header to: {httpClient.DefaultRequestHeaders.Host}");
                }

                var token = Preferences.Get("auth_token", string.Empty);
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    Debug.WriteLine("DatabaseService: Added auth token to request headers");
                }

                using var _ = httpClient; // Ensure disposal
             
              var userIdString = await SecureStorage.GetAsync("userId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    Debug.WriteLine("DatabaseService: userId not found in secure storage");
                    throw new InvalidOperationException("User ID not found in secure storage.");
                }
                Guid userId = Guid.Parse(userIdString);
                Debug.WriteLine($"DatabaseService: Using userId {userId}");

                var response = await httpClient.GetAsync("api/DxfFiles/GetFiles");
                Debug.WriteLine($"DatabaseService: Server responded with status {response.StatusCode}"); if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"DatabaseService: Failed to fetch dxffiles: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                // Ensure entities is never null to avoid CS8602 when iterating
                var entities = JsonConvert.DeserializeObject<List<Root>>(json) ?? new List<Root>();
                Debug.WriteLine($"DatabaseService: Received {entities.Count} files from server");

                foreach (var fileFromServer in entities)
                {
                    if (fileFromServer == null)
                    {
                        Debug.WriteLine("DatabaseService: Skipping null fileFromServer entry");
                        continue;
                    }

                    try
                    {
                        var resourceName = fileFromServer.storedFileName != null ? fileFromServer.storedFileName.Trim() : string.Empty;

                        var existingFile = await _database.Table<DxfFileEntry>()
                            .Where(f => f.ResourceName == resourceName && f.AppUserId == userId)
                            .FirstOrDefaultAsync();

                        if (existingFile != null)
                        {
                            Debug.WriteLine($"DatabaseService: DxfFileEntry already exists ResourceName={resourceName} Id={existingFile.Id}");
                            continue;
                        }

                        var newFileEntry = new DxfFileEntry
                        {
                            Name = fileFromServer.fileName != null ? fileFromServer.fileName.Trim() : string.Empty,
                            ResourceName = resourceName,
                            AppUserId = userId,
                            ReferenceDrawingId = 0L
                        };

                        // Safely convert ReferenceDrawingId if present and in range
                        try
                        {
                            // If ReferenceDrawingId is nullable or a numeric type, handle appropriately
                            // Use dynamic checks to avoid compile-time assumptions about Root
                            var refValue = fileFromServer.ReferenceDrawingId;
                            if (refValue != null)
                            {
                                // Attempt to convert to long safely
                                try
                                {
                                    long refLong = Convert.ToInt64(refValue);
                                    newFileEntry.ReferenceDrawingId = refLong;
                                }
                                catch (OverflowException)
                                {
                                    Debug.WriteLine($"DatabaseService: ReferenceDrawingId out of Int64 range: {refValue}; defaulting to 0.");
                                    newFileEntry.ReferenceDrawingId = 0L;
                                }
                                catch (Exception)
                                {
                                    Debug.WriteLine($"DatabaseService: Unable to convert ReferenceDrawingId: {refValue}; defaulting to 0.");
                                    newFileEntry.ReferenceDrawingId = 0L;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"DatabaseService: Error reading ReferenceDrawingId: {ex.Message}");
                        }
                        if (newFileEntry.ReferenceDrawingId == 0)
                        {
                            Debug.WriteLine($"DatabaseService: Error reading ReferenceDrawingId value 0");
                           
                        }
                        await _database.InsertAsync(newFileEntry);
                        Debug.WriteLine($"DatabaseService: Inserted new DxfFileEntry ResourceName={resourceName} Id={newFileEntry.Id}");

                        // Optionally fetch background resource (await properly); don't insert the raw string as a DB row.
                        try
                        {
                            var resource = new ResourceAccess();
                            var backgroundString = await resource.GetBackgroundResourceAsync(newFileEntry.ReferenceDrawingId);
                            Debug.WriteLine($"DatabaseService: Fetched background resource for RefId={newFileEntry.ReferenceDrawingId} (length={backgroundString?.Length ?? 0})");
                            // If you need to persist the background content, save to file or a dedicated table here.

                            MemoryStream ms = new MemoryStream();
                            await ms.ReadAsync(System.Text.Encoding.UTF8.GetBytes(backgroundString ?? string.Empty));

                            var outentity = ms.ToArray();
                            if (outentity != null)
                            {
                                _filesPathRef += "\\" + resourceName;

                                File.WriteAllBytes(_filesPathRef, outentity);
                                Debug.WriteLine($"DatabaseService: Wrote resource to file {_filesPathRef}");
                            }


                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"DatabaseService: Error fetching background resource: {ex.Message}");
                        }

                        var referenceDrawing = new ReferenceDrawing
                        {


                            Name = newFileEntry.Name,
                            DxfFileId = newFileEntry.Id
                        };
                        await _database.InsertAsync(referenceDrawing);
                        Debug.WriteLine($"DatabaseService: Inserted ReferenceDrawing Id={referenceDrawing.Id} for DxfFileEntry Id={newFileEntry.Id}");
                    }
                    catch (SQLiteException sx)
                    {
                        Debug.WriteLine($"DatabaseService: SQLiteException inserting ResourceName={fileFromServer?.storedFileName ?? "<unknown>"} : {sx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DatabaseService: Exception inserting ResourceName={fileFromServer?.storedFileName ?? "<unknown>"} : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DatabaseService: Exception in CopyFilesFromServerToLocalDb: {ex}");
                Console.WriteLine($"DatabaseService: Exception in CopyFilesFromServerToLocalDb: {ex}");
                throw;
            }
            finally
            {
                Debug.WriteLine("DatabaseService: CopyFilesFromServerToLocalDb completed");
            }
            return;
        }

        public Task<int> GetRecordCount()
        {
            Debug.WriteLine("DatabaseService: GetRecordCount called");
            return _database.Table<DxfFileEntry>().CountAsync();
        }

        public async Task<List<DxfFileEntry>> GetDxfFilesAsync(Guid userId)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFilesAsync userId={userId}");
            return await _database.Table<DxfFileEntry>().Where(f => f.AppUserId == userId).ToListAsync();
        }

        public async Task<DxfFileEntry?> GetDxfFileByNameAsync(string resourceName)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFileByNameAsync resourceName={resourceName}");
            return await _database.Table<DxfFileEntry>()
                .Where(f => f.ResourceName == resourceName)
                .FirstOrDefaultAsync();
        }
        public async Task<int> GetDxfFileIdAsync(string resourceName)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFileIdAsync resourceName={resourceName}");
            var result = await _database.Table<DxfFileEntry>()
                .Where(f => f.ResourceName == resourceName)
                .FirstOrDefaultAsync();

            return result.Id;
        }
        public async Task<DxfFile> GetDxfFileByReferenceDrawingIdAsync(long referenceDrawingId)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFileByReferenceDrawingIdAsync ReferenceDrawingId={referenceDrawingId}");
            var result = await _database.Table<DxfFileEntry>()
                .Where(f => f.ReferenceDrawingId == referenceDrawingId)
                .FirstOrDefaultAsync();

            DxfFile dxfFile = new DxfFile
            {
                Id = result.Id,
                FileName = result.Name,
                StoredFileName = result.ResourceName,
                AppUserId = result.AppUserId.ToString(),
                ReferenceDrawingId = (int)result.ReferenceDrawingId
            };


            return dxfFile;
        }
        public async Task<DxfFileEntry?> GetDxfFileBytesAsync(string resourceName)
        {
            Debug.WriteLine($"DatabaseService: GetDxfFileBytesAsync resourceName={resourceName}");
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
                        Debug.WriteLine($"DatabaseService: Wrote resource to file {resourceName}");
                    }

                    var fileEntry = new DxfFileEntry
                    {
                        Id = entity.id,
                        Name = entity.fileName,
                        ResourceName = entity.storedFileName,
                        AppUserId = userId,
                        ReferenceDrawingId = entity.ReferenceDrawingId
                    };
                    Debug.WriteLine($"DatabaseService: GetDxfFileBytesAsync returning DxfFileEntry Id={fileEntry.Id}");
                    return fileEntry;
                }
                Debug.WriteLine("DatabaseService: GetDxfFileBytesAsync entity == null");
                return null;
            }
            Debug.WriteLine($"DatabaseService: GetDxfFileBytesAsync response failed {response.StatusCode}");
            return null;
        }

        // Add these methods to the existing DatabaseService class

        public async Task SaveLayerGroupStateAsync(Guid userId, string dxfFileName, IEnumerable<LayerItem> layers)
        {
            await InitializeAsync();

            // Delete existing states for this file
            await _database.ExecuteAsync(
                "DELETE FROM LayerGroupStates WHERE UserId = ? AND DxfFileName = ?",
                userId, dxfFileName);

            // Insert new states
            var states = layers.Select(layer => new LayerGroupState
            {
                UserId = userId,
                DxfFileName = dxfFileName,
                LayerName = layer.LayerName,
                LayerIndex = layer.LayerIndex,
                IsVisible = layer.IsVisible,
                ColorR = (int)(layer.color.Red * 255),
                ColorG = (int)(layer.color.Green * 255),
                ColorB = (int)(layer.color.Blue * 255),
                ColorA = (int)(layer.color.Alpha * 255),
                LastModified = DateTime.UtcNow
            }).ToList();

            await _database.InsertAllAsync(states);
        }

        public async Task<List<LayerGroupState>> LoadLayerGroupStateAsync(Guid userId, string dxfFileName)
        {
            await InitializeAsync();

            return await _database.Table<LayerGroupState>()
                .Where(s => s.UserId == userId && s.DxfFileName == dxfFileName)
                .OrderBy(s => s.LayerIndex)
                .ToListAsync();
        }

        public async Task SavePolylineStatesAsync(Guid userId, string dxfFileName, string layerName, IEnumerable<Pline2DModel> polylines)
        {
            await InitializeAsync();

            // Delete existing states for this layer
            await _database.ExecuteAsync(
                "DELETE FROM PolylineStates WHERE UserId = ? AND DxfFileName = ? AND LayerName = ?",
                userId, dxfFileName, layerName);

            // Insert new states
            var states = polylines.Select((pline, index) => new PolylineState
            {
                UserId = userId,
                DxfFileName = dxfFileName,
                LayerName = layerName,
                PolylineIndex = index,
                IsPainted = pline.IsPainted,
                LastModified = DateTime.UtcNow
            }).ToList();

            if (states.Any())
            {
                await _database.InsertAllAsync(states);
            }
        }

        public async Task<List<PolylineState>> LoadPolylineStatesAsync(Guid userId, string dxfFileName, string layerName)
        {
            await InitializeAsync();

            return await _database.Table<PolylineState>()
                .Where(s => s.UserId == userId && s.DxfFileName == dxfFileName && s.LayerName == layerName)
                .OrderBy(s => s.PolylineIndex)
                .ToListAsync();
        }

        public async Task ClearAllStatesAsync(Guid userId, string dxfFileName)
        {
            await InitializeAsync();

            await _database.ExecuteAsync(
                "DELETE FROM LayerGroupStates WHERE UserId = ? AND DxfFileName = ?",
                userId, dxfFileName);

            await _database.ExecuteAsync(
                "DELETE FROM PolylineStates WHERE UserId = ? AND DxfFileName = ?",
                userId, dxfFileName);
        }

    }
}