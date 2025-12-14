using Core.Business.Objects;
using Core.Business.Objects.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace NumberArtistView.Services
{
    public class ResourceAccess
    {
        private readonly HttpClient _httpClient;
        private SQLiteAsyncConnection _database;

        // Cache JsonSerializerOptions instance for reuse (CA1869 fix)
        private static readonly System.Text.Json.JsonSerializerOptions CachedJsonOptions =
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ResourceAccess()
        {

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "NumberArtist.db3");
            Debug.WriteLine($"DatabaseService: DB path = {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);

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

        //
        public async Task<byte[]?> GetResourceAsync(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            // Sanitize filename to avoid path traversal
            var safeName = Path.GetFileName(resourceName);

            var targetDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "dxf_files");
            Directory.CreateDirectory(targetDirectory);

            var targetFile = Path.Combine(targetDirectory, safeName);

            // If cached on disk, return bytes directly
            if (File.Exists(targetFile))
            {
                try
                {
                    return await File.ReadAllBytesAsync(targetFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading cached resource file: {ex.Message}");
                    // fall through to re-fetch from server
                }
            }

            try
            {
                // Request headers-first so we can stream the body
                var requestUri = $"api/dxffiles/GetResource?resourceName={Uri.EscapeDataString(safeName)}";
                using var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Stream response to disk to avoid large in-memory buffers
                await using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    // Write to a temp file first then move to final path to avoid partial files on failures
                    var tempFile = targetFile + ".tmp";
                    await using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        await responseStream.CopyToAsync(fs);
                        await fs.FlushAsync();
                    }

                    // Atomically replace final file
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    File.Move(tempFile, targetFile);
                }

                // Return the bytes we just saved
                return await File.ReadAllBytesAsync(targetFile);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching resource: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return null;
            }
        }


        public async Task<string> GetBackGroundResourceBynameAsync(string resourceName)
        {
            // 1. Define the target directory and file path within the app's data folder.
            var targetDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files");
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
                var response = await _httpClient.GetAsync($"api/dxffiles/GetBackGroundResource?resourceName={resourceName}");
                response.EnsureSuccessStatusCode(); // Throws an exception for non-2xx responses.
                                                    //check api callhkjhkjhkjh
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


        public async Task<string?> GetBackgroundResourceAsync(long ReferencefileId)
        {
            if (ReferencefileId <= 0)
                return null;

            var targetDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files");
            //           if (!Directory.Exists(targetDirectory))
            //{
            //               Directory.CreateDirectory(targetDirectory);
            //           }

            // Try to get the reference drawing from local DB
            ReferenceDrawing dxfReferenceEntry = null;
            try
            {

                if (ReferencefileId <= int.MaxValue)
                {
                    dxfReferenceEntry = await _database.FindAsync<ReferenceDrawing>((int)ReferencefileId);
                }
                //dxfReferenceEntry = await _database.Table<ReferenceDrawing>()
                //                 .Where(f => f.Id == (int)ReferencefileId)
                //                 .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {

                string error = ex.Message;
                dxfReferenceEntry = null;
            }
            if (dxfReferenceEntry == null)
            {  //get from server}


                try
                {

                    //?resourceName={Uri.EscapeDataString(filename)}

              
                    var response = await _httpClient.GetAsync($"api/Dxffiles/GetDxfReferenceDrawingByReferenceId/{ReferencefileId}");


          
                    response.EnsureSuccessStatusCode();
                  

                    var content = await response.Content.ReadAsStringAsync();
                    var refdrawing = Newtonsoft.Json.JsonConvert.DeserializeObject<ReferenceDrawing>(content);

                    // Deserialize into ReferenceDrawing (case-insensitive)
                    // Use cached JsonSerializerOptions instance

                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var obj = JsonSerializer.Deserialize<ReferenceDrawing>(bytes, CachedJsonOptions);
                    dxfReferenceEntry = System.Text.Json.JsonSerializer.Deserialize<ReferenceDrawing>(content, CachedJsonOptions);

                    if (dxfReferenceEntry != null)
                    {
                        // Cache the result in the local database
                    
                        await _database.InsertOrReplaceAsync(dxfReferenceEntry);



                        var responseBackground = await _httpClient.GetAsync($"api/Dxffiles/GetReferenceImage/{ReferencefileId}");

                        //var dfd = await responseBackground.Content.ReadAsByteArrayAsync();
                        //MemoryStream ms = new MemoryStream(dfd);

                        //File.WriteAllBytes(filepath);
                        //responseBackground.EnsureSuccessStatusCode();
                        var contentBackground = await responseBackground.Content.ReadAsByteArrayAsync();
                        string gjhj = "stop and check";

                        // Fetch the DXF resource content and write to ref_files
                        //error looping   var resourceContent = await GetBackgroundResourceAsync(dxfReferenceEntry.Id);
                        Console.WriteLine("dxfReferenceEntry.Name" + dxfReferenceEntry.Name);
                       // Console.WriteLine("resourceContent" + resourceContent);
                        if (contentBackground != null)
                        {
                            var refDir = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files", dxfReferenceEntry.Name.ToString());
                            Directory.CreateDirectory(refDir);
                            var targetFileRef = Path.Combine(refDir, dxfReferenceEntry.Name);
                          await File.WriteAllBytesAsync(targetFileRef, contentBackground ?? Array.Empty<byte>());
                            return "done";
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching reference drawing: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        return null;
                }
                return null;
            }

            else
            {

                var filename = dxfReferenceEntry.Name;
                var storedFileName = dxfReferenceEntry.StoredFileName;

                var refDir = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files", dxfReferenceEntry.Name.ToString());

                var refDir_filename = Path.Combine(refDir, dxfReferenceEntry.StoredFileName);

                if (File.Exists(refDir_filename))
                {
                    return await File.ReadAllTextAsync(refDir_filename);
                }
                else
                {
                    try
                    {
                        var response = await _httpClient.GetAsync($"api/dxffiles/GetBackGroundResource?resourceName={Uri.EscapeDataString(filename)}");
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        await File.WriteAllTextAsync(refDir_filename, content);

                        writeconenttolocalpath(dxfReferenceEntry, content);









                        return content;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Error fetching resource: {ex.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        return null;
                    }
                }

            }



            // Helper to write fetched DXF content into ref_files/<DxfFileId>/<Name>
            async Task<string?> WriteRefFileAsync(ReferenceDrawing rd, string fileContent)
            {
                if (rd == null || string.IsNullOrEmpty(rd.Name))
                    return null;

                var refDir = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files", rd.DxfFileId.ToString());
                Directory.CreateDirectory(refDir);
                var targetFileRef = Path.Combine(refDir, rd.Name);
                await File.WriteAllTextAsync(targetFileRef, fileContent ?? string.Empty);

                return targetFileRef;
            }

            if (dxfReferenceEntry != null)
            {
                // Fetch the DXF resource and write to ref_files
                var fileBytes = await GetResourceAsync(dxfReferenceEntry.Name);


                if (fileBytes != null && fileBytes.Length > 0)
                {
                    // Convert byte[] to string (assuming text content, e.g., DXF file is text)
                    var fileContent = Encoding.UTF8.GetString(fileBytes);
                    await WriteRefFileAsync(dxfReferenceEntry, fileContent);
                }

                var resourceName = dxfReferenceEntry.Name;
                var targetFile = Path.Combine(targetDirectory, resourceName);

                if (File.Exists(targetFile))
                    return await File.ReadAllTextAsync(targetFile);

                try
                {
                    var response = await _httpClient.GetAsync($"api/dxffiles/GetBackGroundResourceBynameAsync?resourceName={Uri.EscapeDataString(resourceName)}");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    await File.WriteAllTextAsync(targetFile, content);
                    return content;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching resource: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    return null;
                }
            }
            else
            {
                // Not found locally — consult server / other DB methods
                DatabaseService dbs = new DatabaseService();
                var dxffile = await dbs.GetDxfFileByReferenceDrawingIdAsync(ReferencefileId);
                if (dxffile == null)
                    return null;

                try
                {
                    var response = await _httpClient.GetAsync($"api/Dxffiles/GetDxfReferenceDrawingByReferenceId/{ReferencefileId}");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();

                    // Deserialize into ReferenceDrawing (case-insensitive)
                    // Use cached JsonSerializerOptions instance
                    var referenceDrawing = System.Text.Json.JsonSerializer.Deserialize<ReferenceDrawing>(
                        content,
                        CachedJsonOptions);

                    if (referenceDrawing == null)
                        return null;

                    // Fetch the DXF resource content and write to ref_files


                    var resourceContent = await GetBackgroundResourceAsync(referenceDrawing.Id);
                    if (!string.IsNullOrEmpty(resourceContent))
                    {
                        await WriteRefFileAsync(referenceDrawing, resourceContent);
                    }

                    // Insert into local DB (avoid duplicate key exceptions)
                    try
                    {
                        await _database.InsertAsync(referenceDrawing);
                    }
                    catch
                    {
                        // If insert fails (e.g., already exists), ignore.
                    }

                    // Ensure background file is stored locally and return it
                    var targetFile = Path.Combine(targetDirectory, referenceDrawing.Name);
                    if (File.Exists(targetFile))
                        return await File.ReadAllTextAsync(targetFile);

                    try
                    {
                        var bgResponse = await _httpClient.GetAsync($"api/dxffiles/GetBackGroundResource?resourceName={Uri.EscapeDataString(referenceDrawing.Name)}");
                        bgResponse.EnsureSuccessStatusCode();

                        var bgContent = await bgResponse.Content.ReadAsStringAsync();
                        await File.WriteAllTextAsync(targetFile, bgContent);
                        return bgContent;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Error fetching background resource: {ex.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching reference drawing: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<string> GetBackgroundResourceAsync(string resourceName)

        {
            var response = await _httpClient.GetAsync($"api/dxffiles/GetBackGroundResource?resourceName={Uri.EscapeDataString(resourceName)}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
        private void writeconenttolocalpath(ReferenceDrawing dxfReferenceEntry, string content)
        {

            var refDir = Path.Combine(FileSystem.Current.AppDataDirectory, "ref_files", dxfReferenceEntry.StoredFileName);
            Directory.CreateDirectory(refDir);
            var targetFileRef = Path.Combine(refDir, dxfReferenceEntry.StoredFileName);
            File.WriteAllText(targetFileRef, content ?? string.Empty);
        }

        // Diagnostic helper: call this from a debug path to inspect the DB and try alternative lookup.
        public async Task DiagnoseReferenceDrawingLookupAsync(long referenceFileId)
        {
            Debug.WriteLine($"Diagnose: referenceFileId (long) = {referenceFileId}");
            if (referenceFileId <= int.MaxValue)
                Debug.WriteLine($"Diagnose: cast to int = {(int)referenceFileId}");
            else
                Debug.WriteLine("Diagnose: referenceFileId larger than int.MaxValue");

            // DB path info
            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "NumberArtist.db3");
                Debug.WriteLine($"Diagnose: DB path = {dbPath}");
                Debug.WriteLine($"Diagnose: DB file exists = {File.Exists(dbPath)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnose: error reading DB path: {ex.Message}");
            }

            // Ensure table exists (won't delete data; CreateTableAsync is safe)
            try
            {
                await _database.CreateTableAsync<ReferenceDrawing>();
                var total = await _database.Table<ReferenceDrawing>().CountAsync();
                Debug.WriteLine($"Diagnose: ReferenceDrawing table row count = {total}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnose: error ensuring table / counting rows: {ex.Message}");
            }

            //// Try FindAsync
            //try
            //{
            //    if (referenceFileId <= int.MaxValue)
            //    {
            //        var byFind = await _database.FindAsync<ReferenceDrawing>((int)referenceFileId);
            //        Debug.WriteLine(byFind == null ? "Diagnose: FindAsync returned null" : $"Diagnose: FindAsync found Id={byFind.Id} Name={byFind.Name}");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine($"Diagnose: FindAsync threw: {ex.Message}");
            //}

            // Try explicit query
            try
            {
                var byQuery = await _database.Table<ReferenceDrawing>().Where(r => r.Id == (long)referenceFileId).FirstOrDefaultAsync();
                Debug.WriteLine(byQuery == null ? "Diagnose: Table.Where returned null" : $"Diagnose: Table.Where found Id={byQuery.Id} Name={byQuery.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnose: Table.Where threw: {ex.Message}");
            }

            // Dump a few sample rows to inspect id values (first 10)
            try
            {
                var sample = await _database.Table<ReferenceDrawing>().Take(10).ToListAsync();
                Debug.WriteLine($"Diagnose: sample count = {sample.Count}");
                foreach (var r in sample)
                    Debug.WriteLine($"Diagnose: row Id={r.Id} Name={r.Name} StoredFileName={r.StoredFileName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnose: sample dump threw: {ex.Message}");
            }
        }
    }
}