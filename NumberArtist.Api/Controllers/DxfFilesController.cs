using Core.Business.Objects;
using Core.Business.Objects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Configuration;
using NumberArtist.Api.Data;
using NumberArtist.Api.Migrations;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberArtist.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // This secures all actions in this controller
    public class DxfFilesController : ControllerBase
    {
        private readonly string _filesPath;
        private readonly string _filesPathRef;

        private readonly ApplicationDbContext _context;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedImageContentTypes;

        public DxfFilesController(IWebHostEnvironment env, ApplicationDbContext context, IConfiguration configuration)
        {
            _filesPath = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "dxf_files");
            _filesPathRef = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "ref_files");

            // Ensure the directory exists.
            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath);
            }
            if (!Directory.Exists(_filesPathRef))
            {
                Directory.CreateDirectory(_filesPathRef);
            }
            _context = context;

            // Read configuration with safe fallbacks.
            // Configuration keys:
            // FileUpload:MaxFileSizeMB (int) - maximum allowed file size in megabytes
            // FileUpload:AllowedImageContentTypes (array) - list of allowed image content types
            var maxFileSizeMb = configuration.GetValue<int?>("FileUpload:MaxFileSizeMB") ?? 10;
            _maxFileSizeBytes = Math.Max(1, maxFileSizeMb) * 1024L * 1024L;

            _allowedImageContentTypes = configuration
                .GetSection("FileUpload:AllowedImageContentTypes")
                .Get<string[]>()
                ?? new[] { "image/jpeg", "image/pjpeg" };
        }

        [HttpGet("GetFiles")]
        [ProducesResponseType(typeof(IEnumerable<DxfFile>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFiles()
        {
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);

            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var files = _context.DxfFiles.Where(f => f.AppUserId == userToken.UserId);
            return Ok(files);
        }
        //[HttpGet("GetBackgroundFiles")]
        //[ProducesResponseType(typeof(IEnumerable<ReferenceDrawing>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetBackgroundFiles()
        //{
        //    var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        //    var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);

        //    if (userToken == null)
        //    {
        //        return Unauthorized("Invalid or missing auth token.");
        //    }

        //    var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;


        //    var refUserFolder = Path.Combine(_filesPathRef, userName);


        //   var files = System.IO.Directory.GetFiles(refUserFolder);

        //    return Ok(files);
        //}

        [HttpGet("GetReferenceDrawings")]
        [ProducesResponseType(typeof(IEnumerable<ReferenceDrawing>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReferenceDrawings()
        {
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);

            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var tt = _context.DxfFiles.Where(f => f.AppUserId == userToken.UserId);

            var files = new List<ReferenceDrawing>();
            foreach (var dxf in tt)
            {
                ReferenceDrawing referenceDrawing = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.Id == dxf.ReferenceDrawingId);

                if (referenceDrawing != null)
                {
                    files.Add(referenceDrawing);
                }
            }

            return Ok(files);
        }

        [HttpGet("GetResource")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResource(string resourceName)
        {
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("Could not determine user identity.");
            }

            var userFolderPath = Path.Combine(_filesPath, userName);
            var filePath = Path.Combine(userFolderPath, resourceName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File not found: {resourceName}");
            }
           
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", resourceName);
        }

        [HttpGet("GetBackGroundResource")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBackGroundResource(string resourceName)
        {
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("Could not determine user identity.");
            }

            var userFolderPath = Path.Combine(_filesPath, userName);
           


            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return BadRequest("The user associated with this action does not exist.");
            }
            var filePath = Path.Combine(userFolderPath, resourceName);

            // Load the associated DXF
               // Load the reference drawing
            var reference = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.Name == resourceName);
            if (reference == null)
            {
                return NotFound($"ReferenceDrawing with name {resourceName} not found.");
            }



            if (resourceName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || resourceName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var userId = user.Id +"_";
                userFolderPath = Path.Combine(_filesPathRef, userName);
                filePath = Path.Combine(userFolderPath, reference.StoredFileName);
            }
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var file = File(fileBytes, "application/octet-stream", resourceName);
            return Ok(file);
        }


        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DxfFile), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile(IFormFile file, int? dxfId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                return BadRequest($"File too large. Maximum allowed is {_maxFileSizeBytes / (1024 * 1024)} MB.");
            }

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext != ".dxf" && ext != ".jpg" && ext != ".jpeg")
            {
                return BadRequest("Only .dxf or .jpg files are allowed.");
            }

            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("Could not determine user identity.");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return BadRequest("The user associated with this action does not exist.");
            }

            // Basic MIME type validation for images
            if (ext == ".jpg" || ext == ".jpeg")
            {
                if (!string.IsNullOrEmpty(file.ContentType) && !_allowedImageContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                {
                    // ContentType may be missing/incorrect from some clients; still perform signature check below.
                    // We allow proceeding to signature check but warn client if content type appears wrong.
                }
            }

            // Signature/content checks
            if (ext == ".jpg" || ext == ".jpeg")
            {
                if (!await IsValidJpegAsync(file))
                    return BadRequest("Uploaded file is not a valid JPEG image.");
            }
            else // .dxf
            {
                if (!await LooksLikeDxfAsync(file))
                    return BadRequest("Uploaded file does not appear to be a valid DXF.");
            }

            string userFolderPath = UserPath(userName);
            if (ext == ".jpg" || ext == ".jpeg")
            {
                userFolderPath = Path.Combine(_filesPathRef, userName);
            }
            else
            {
                userFolderPath = Path.Combine(_filesPath, userName);
            }

            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            // Sanitize the filename to prevent path traversal attacks.
            var fileName = Path.GetFileName(file.FileName);
            var storedFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(userFolderPath, storedFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Use a transaction when creating/updating related entities
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                if (ext == ".jpg" || ext == ".jpeg")
                {
                    // For background uploads we require the client to provide dxfId so we can link them.
                    if (!dxfId.HasValue)
                    {
                        await tx.RollbackAsync();
                        System.IO.File.Delete(filePath);
                        return BadRequest("dxfId is required when uploading a background (.jpg) file.");
                    }

                    // Validate ownership and existence of the DXF being referenced
                    var targetDxf = await _context.DxfFiles.FirstOrDefaultAsync(d => d.Id == dxfId.Value && d.AppUserId == user.Id);
                    if (targetDxf == null)
                    {
                        await tx.RollbackAsync();
                        System.IO.File.Delete(filePath);
                        return BadRequest("Referenced DXF not found or not owned by user.");
                    }

                    // Prevent duplicate ReferenceDrawing for the same DXF
                    var existingRef = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.DxfFileId == targetDxf.Id);
                    if (existingRef != null)
                    {
                        await tx.RollbackAsync();
                        System.IO.File.Delete(filePath);
                        return Conflict("A background has already been associated with this DXF.");
                    }

                    // Create ReferenceDrawing that points to the DXF
                  
                    //get  guid
                    var reference = new ReferenceDrawing
                    {
                        Name = fileName,
                        DxfFileId = targetDxf.Id,
                        StoredFileName   = filePath

                    };
                    _context.ReferenceDrawings.Add(reference);
                    await _context.SaveChangesAsync(); // reference.Id populated

                    // Create a DxfFile record for the uploaded JPG so it appears in DxfFiles list
                    var jpgEntry = new DxfFile
                    {
                        FileName = fileName,
                        StoredFileName = storedFileName,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow,
                        AppUserId = user.Id,
                        ReferenceDrawingId = reference.Id
                    };
                    _context.DxfFiles.Add(jpgEntry);

                    // Link the existing DXF to the reference
                    targetDxf.ReferenceDrawingId = reference.Id;
                    _context.DxfFiles.Update(targetDxf);

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return CreatedAtAction(nameof(GetFiles), new { id = jpgEntry.Id }, jpgEntry);
                }
                else
                {
                    // .dxf upload: create the DXF entity
                    var dxfFile = new DxfFile
                    {
                        FileName = fileName,
                        StoredFileName = storedFileName,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow,
                        AppUserId = user.Id
                    };

                    _context.DxfFiles.Add(dxfFile);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return CreatedAtAction(nameof(GetFiles), new { id = dxfFile.Id }, dxfFile);
                }
            }
            catch
            {
                // If anything fails, ensure transaction is rolled back and delete the saved file to avoid orphan files
                await tx.RollbackAsync();
                try { System.IO.File.Delete(filePath); } catch { /* ignore file delete errors */ }
                throw;
            }
        }

        // Lightweight JPEG signature check (SOI bytes 0xFF 0xD8 0xFF)
        private static async Task<bool> IsValidJpegAsync(IFormFile file)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                if (stream.Length < 3) return false;
                var buffer = new byte[3];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                return read == 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
            }
            catch
            {
                return false;
            }
        }

        // Lightweight DXF content heuristic: read first N bytes and look for DXF-typical tokens
        private static async Task<bool> LooksLikeDxfAsync(IFormFile file)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var lengthToRead = (int)Math.Min(4096, stream.Length);
                if (lengthToRead <= 0) return false;
                var buffer = new byte[lengthToRead];
                var read = await stream.ReadAsync(buffer, 0, lengthToRead);
                var text = Encoding.ASCII.GetString(buffer, 0, read);
                // Common DXF tokens/markers: "SECTION", "ENTITIES", "HEADER", "ACAD"
                return text.IndexOf("SECTION", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("ENTITIES", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("HEADER", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("ACAD", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("/UploadPair")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DxfFile), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFilePair(IFormFile file1, IFormFile file2)
        {
            /*
            PSEUDOCODE / PLAN (detailed):
            1. Validate presence and basic sizes of both files.
            2. Validate extensions: file1 must be .dxf, file2 must be .jpg/.jpeg.
            3. Validate file sizes against _maxFileSizeBytes.
            4. Authenticate user similarly to UploadFile:
               - read bearer token from Authorization header
               - find matching UserToken in _context
               - get userName from claims and load AppUser from _context
            5. Perform lightweight content checks:
               - For .dxf use LooksLikeDxfAsync
               - For .jpg use IsValidJpegAsync
            6. Prepare storage paths:
               - dxf user folder: Path.Combine(_filesPath, userName)
               - jpg user folder: Path.Combine(_filesPathRef, userName)
               - ensure directories exist
            7. Sanitize incoming file names with Path.GetFileName and build storedFileName with Guid prefix.
            8. Begin a DB transaction.
            9. Save file1 to dxf folder, create a DxfFile entity and save to DB.
            10. Check whether a ReferenceDrawing already exists for the created DXF (prevent duplicates).
            11. Save file2 to ref folder, create a ReferenceDrawing entity that references the created DXF.
                 - Do NOT create a DxfFile record for the JPG. (This fixes the bug where both entities end up in DxfFiles.)
            12. Update the created DxfFile.ReferenceDrawingId to the new ReferenceDrawing.Id and save changes.
            13. Commit transaction. Return a response that includes both created records (DXF and ReferenceDrawing).
            14. On any failure, rollback transaction and remove any saved files to avoid orphans.
            */

            // 1-3: basic validation
            if (file1 == null || file1.Length == 0 || file2 == null || file2.Length == 0)
            {
                return BadRequest("Both files are required.");
            }

            if (file1.Length > _maxFileSizeBytes || file2.Length > _maxFileSizeBytes)
            {
                return BadRequest($"One or both files exceed the maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)} MB.");
            }

            var ext1 = Path.GetExtension(file1.FileName)?.ToLowerInvariant();
            var ext2 = Path.GetExtension(file2.FileName)?.ToLowerInvariant();

            if (ext1 != ".dxf")
            {
                return BadRequest("First file must be a .dxf.");
            }
            if (ext2 != ".jpg" && ext2 != ".jpeg")
            {
                return BadRequest("Second file must be a .jpg or .jpeg.");
            }

            // 4: authenticate & load user
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("Could not determine user identity.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return BadRequest("The user associated with this action does not exist.");
            }

            // 5: signature/content checks
            if (!await LooksLikeDxfAsync(file1))
            {
                return BadRequest("Uploaded file1 does not appear to be a valid DXF.");
            }
            if (!await IsValidJpegAsync(file2))
            {
                return BadRequest("Uploaded file2 is not a valid JPEG image.");
            }

            // 6: prepare folders
            var dxfUserFolder = Path.Combine(_filesPath, userName);
            var refUserFolder = Path.Combine(_filesPathRef, userName);
            if (!Directory.Exists(dxfUserFolder)) Directory.CreateDirectory(dxfUserFolder);
            if (!Directory.Exists(refUserFolder)) Directory.CreateDirectory(refUserFolder);

            // 7: sanitize and prepare stored names
            var file1Name = Path.GetFileName(file1.FileName);
            var storedFile1Name = $"{Guid.NewGuid()}_{file1Name}";
            var file1Path = Path.Combine(dxfUserFolder, storedFile1Name);

            var file2Name = Path.GetFileName(file2.FileName);
            var storedFile2Name = $"{Guid.NewGuid()}_{file2Name}";
            var file2Path = Path.Combine(refUserFolder, storedFile2Name);

            // 8: transaction
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 9: save file1 to disk, then create DxfFile entity
                await using (var stream1 = new FileStream(file1Path, FileMode.Create))
                {
                    await file1.CopyToAsync(stream1);
                }

                var dxfEntry = new DxfFile
                {
                    FileName = file1Name,
                    StoredFileName = storedFile1Name,
                    ContentType = file1.ContentType,
                    UploadedAt = DateTime.UtcNow,
                    AppUserId = user.Id
                };
                _context.DxfFiles.Add(dxfEntry);
                await _context.SaveChangesAsync(); // dxfEntry.Id populated

                // 10: ensure no existing reference for this DXF
                var existingRef = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.DxfFileId == dxfEntry.Id);
                if (existingRef != null)
                {
                    // rollback, remove saved dxf file
                    await tx.RollbackAsync();
                    try { System.IO.File.Delete(file1Path); } catch { /* ignore */ }
                    return Conflict("A background is already associated with this DXF.");
                }

                // 11: save file2 to disk and create ReferenceDrawing only (do NOT create a DxfFile for the JPG)
                await using (var stream2 = new FileStream(file2Path, FileMode.Create))
                {
                    await file2.CopyToAsync(stream2);
                }

                var reference = new ReferenceDrawing
                {
                    Name = file2Name,
                    DxfFileId = dxfEntry.Id
                    ,StoredFileName   = file2Path
                };
                _context.ReferenceDrawings.Add(reference);
                await _context.SaveChangesAsync(); // reference.Id populated

                // 12: update the dxfEntry to point at the reference
                dxfEntry.ReferenceDrawingId = reference.Id;
                _context.DxfFiles.Update(dxfEntry);
                await _context.SaveChangesAsync();

                // 13: commit
                await tx.CommitAsync();

                // Return both created entities (DXF and ReferenceDrawing). JPG is stored on disk but not added to DxfFiles.
                return CreatedAtAction(nameof(GetFiles), new { id = dxfEntry.Id }, new { Dxf = dxfEntry, Reference = reference });
            }
            catch
            {
                await tx.RollbackAsync();
                try { System.IO.File.Delete(file1Path); } catch { /* ignore */ }
                try { System.IO.File.Delete(file2Path); } catch { /* ignore */ }
                throw;
            }
        }

        private string                                                                                                                                                                         UserPath(string userName)
        {
            var userFolderPath = Path.Combine(_filesPath, userName);
            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            return userFolderPath;
        }

        [HttpDelete("{filename}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeleteFile(string filename)
        {
            // Sanitize the filename.
            var sanitizedFileName = Path.GetFileName(filename);
            if (string.IsNullOrEmpty(sanitizedFileName) || sanitizedFileName != filename)
            {
                return BadRequest("Invalid filename.");
            }

            var filePath = Path.Combine(_filesPath, sanitizedFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            System.IO.File.Delete(filePath);

            return NoContent();
        }

        // PSEUDOCODE / DETAILED PLAN:
        // 1. Validate `appUserId` input (not null/empty).
        // 2. Authenticate caller using the Authorization header and _context.UserTokens (consistent with other actions).
        // 3. Query DxfFiles where AppUserId == appUserId to get DXF ids.
        // 4. If no DXFs found return an empty list (200 OK).
        // 5. Query ReferenceDrawings where DxfFileId is in the DXF ids list.
        // 6. Return the list of ReferenceDrawing entities (200 OK).
        // 7. Handle errors by returning appropriate 400/401 responses.
        //
        // This method enumerates DXF records for the provided app user id and
        // returns the referenced drawings associated with those DXFs.

        [HttpGet("ReferencesByUser/{appUserId}")]
        [ProducesResponseType(typeof(IEnumerable<ReferenceDrawing>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetReferenceDrawingsByUser(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId))
            {
                return BadRequest("appUserId is required.");
            }

            // Authenticate similarly to other endpoints in this controller
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            // 3: get DXF ids for the specified user
            var dxfIds = await _context.DxfFiles
                .Where(d => d.AppUserId == appUserId)
                .Select(d => d.Id)
                .ToListAsync();

            if (dxfIds == null || dxfIds.Count == 0)
            {
                // No DXFs for that user -> return empty list
                return Ok(Enumerable.Empty<ReferenceDrawing>());
            }

            // 5: query the ReferenceDrawings that reference those DXFs
            var referenceDrawings = await _context.ReferenceDrawings
                .Where(r => dxfIds.Contains(r.DxfFileId))
                .ToListAsync();



            return Ok(referenceDrawings);
        }
        [HttpGet("GetDxfFileByReferenceId/{referenceDrawingId}")]
        [ProducesResponseType(typeof(ReferenceDrawing), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDxfFileByReferenceId(int referenceDrawingId)
        {
            /*
            PSEUDOCODE / DETAILED PLAN:
            1. Validate the input id (must be > 0). If invalid return BadRequest.
            2. Read the bearer token from the Authorization header and look up the corresponding UserToken in the database.
               - If no matching UserToken exists, return Unauthorized.
            3. Load the ReferenceDrawing entity with the supplied referenceDrawingId.
               - If not found, return NotFound.
            4. Load the DxfFile entity referenced by ReferenceDrawing.DxfFileId.
               - If not found, return NotFound (associated DXF missing).
            5. Verify ownership: ensure the DxfFile.AppUserId matches the authenticated user's id from the UserToken.
               - If mismatch, return Unauthorized (user does not own the resource).
            6. Return the DxfFile entity (200 OK).
            7. Any unexpected exceptions will propagate (controller/global error handling will apply).
            */

            if (referenceDrawingId <= 0)
            {
                return BadRequest("referenceDrawingId is required.");
            }

            // Authenticate using the same pattern as other endpoints
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null)
            {
                return Unauthorized("Invalid or missing auth token.");
            }

            // Load the reference drawing
            var reference = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.Id == referenceDrawingId);
            if (reference == null)
            {
                return NotFound($"ReferenceDrawing with id {referenceDrawingId} not found.");
            }

            // Load the associated DXF
            var dxf = await _context.DxfFiles.FirstOrDefaultAsync(d => d.Id == reference.DxfFileId);
            if (dxf == null)
            {
                return NotFound("Associated DXF not found.");
            }

            // Ensure the authenticated user owns the DXF
            if (!string.Equals(dxf.AppUserId, userToken.UserId, StringComparison.Ordinal))
            {
                return Unauthorized("You do not have access to this resource.");
            }

            return Ok(dxf);
        }

      
        [HttpGet("GetDxfReferenceDrawingByReferenceId/{referenceDrawingId}")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDxfReferenceDrawingByReferenceId(int referenceDrawingId)
        {
            if (referenceDrawingId <= 0) return BadRequest("Invalid reference drawing ID.");

            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null) return Unauthorized("Invalid or missing auth token.");

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName)) return Unauthorized("Could not determine user identity.");

            var reference = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.Id == referenceDrawingId);
            if (reference == null) return NotFound($"ReferenceDrawing with id {referenceDrawingId} not found.");

            var dxf = await _context.DxfFiles.FirstOrDefaultAsync(d => d.Id == reference.DxfFileId);
            if (dxf == null) return NotFound("Associated DXF not found.");
            if (!string.Equals(dxf.AppUserId, userToken.UserId, StringComparison.Ordinal)) return Unauthorized();


            return Ok(reference);
            ////// Determine stored file name/path reliably
            ////var stored = reference.StoredFileName ?? reference.Name;
            ////string filePath;
            ////if (Path.IsPathRooted(stored))
            ////{
            ////    filePath = stored; // stored contains absolute path
            ////}
            ////else
            ////{
            ////    // stored is a filename or relative path
            ////    filePath = Path.Combine(_filesPathRef, userName, Path.GetFileName(stored));
            ////}

            ////if (!System.IO.File.Exists(filePath)) return NotFound("Reference file missing on disk.");
            ////var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            ////return File(fileBytes, "application/octet-stream", reference.Name);
            ///


        }

        [HttpGet("GetReferenceImage/{referenceDrawingId}")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReferenceImage(int referenceDrawingId)
        {
            if (referenceDrawingId <= 0) return BadRequest("Invalid reference drawing ID.");

            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);
            if (userToken == null) return Unauthorized("Invalid or missing auth token.");

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName)) return Unauthorized("Could not determine user identity.");

            var reference = await _context.ReferenceDrawings.FirstOrDefaultAsync(r => r.Id == referenceDrawingId);
            if (reference == null) return NotFound($"ReferenceDrawing with id {referenceDrawingId} not found.");

            var dxf = await _context.DxfFiles.FirstOrDefaultAsync(d => d.Id == reference.DxfFileId);
            if (dxf == null) return NotFound("Associated DXF not found.");

            if (!string.Equals(dxf.AppUserId, userToken.UserId, StringComparison.Ordinal))
            {
                return Unauthorized("You do not have access to this resource.");
            }

            var stored = reference.StoredFileName ?? reference.Name ?? string.Empty;
            string filePath;
            if (Path.IsPathRooted(stored))
            {
                filePath = stored; // absolute path stored in DB
            }
            else
            {
                // stored is a filename or relative path; place under user's ref_files folder
                filePath = Path.Combine(_filesPathRef, userName, Path.GetFileName(stored));
            }

            if (!System.IO.File.Exists(filePath)) return NotFound("Reference file missing on disk.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            var contentType = (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" : "application/octet-stream";
            var downloadName = Path.GetFileName(reference.Name ?? filePath);

            return File(fileBytes, contentType, downloadName);
        }




    }
}
