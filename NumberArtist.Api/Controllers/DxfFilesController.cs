using Core.Business.Objects;
using Core.Business.Objects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumberArtist.Api.Data;
using System.IO;
using System.Linq;
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

        public DxfFilesController(IWebHostEnvironment env, ApplicationDbContext context)
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
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DxfFile>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFiles()
        {
            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);


            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            //var userFolderPath = Path.Combine(_filesPath, userName);

            //var files = Directory.GetFiles(userFolderPath)
            //                     .Select(Path.GetFileName);

          var files=  _context.DxfFiles.Select(f => f).Where(f => f.AppUserId == userToken.UserId);
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
      
        
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DxfFile), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            if (Path.GetExtension(file.FileName)?.ToLowerInvariant() != ".dxf" || Path.GetExtension(file.FileName)?.ToLowerInvariant() != ".jpg")
            {
                return BadRequest("Only .dxf or .jpg files are allowed.");
            }

            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);


            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                // This should ideally not happen if [Authorize] is working correctly,
                // but it's good practice to handle it.
                return Unauthorized("Could not determine user identity.");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return BadRequest("The user associated with this action does not exist.");
            }

            string userFolderPath = UserPath(userName);
            if (Path.GetExtension(file.FileName)?.ToLowerInvariant() == ".jpg")
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


            var dxfFile = new DxfFile
            {
                FileName = fileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                AppUserId = user.Id
            };
            //hjhhj
            _context.DxfFiles.Add(dxfFile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFiles), new { id = dxfFile.Id }, dxfFile);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DxfFile), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFilePair(IFormFile file1, IFormFile file2)
        {
            if (file1 == null || file1.Length == 0 || file2 == null || file2.Length == 0)
            {
                return BadRequest("No files were uploaded.");
            }

            if (Path.GetExtension(file1.FileName)?.ToLowerInvariant() != ".dxf" ||
                Path.GetExtension(file2.FileName)?.ToLowerInvariant() != ".jpg")
            {
                return BadRequest("Only .dxf files are allowed.");
            }

            // Process each file
            var result1 = await UploadFile(file1);

            var result2 = await UploadFile(file2);

            if (result1 is BadRequestObjectResult || result2 is BadRequestObjectResult)
            {
                return BadRequest("One or both files failed to upload.");
            }

            return Ok(new { File1 = result1, File2 = result2 });


            if (Path.GetExtension(file1.FileName)?.ToLowerInvariant() != ".dxf" ||
                Path.GetExtension(file2.FileName)?.ToLowerInvariant() != ".jpg")
            {
                return BadRequest("Only .dxf or jpg files are allowed.");
            }

            var authtoken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var userToken = await _context.UserTokens.FirstOrDefaultAsync(t => t.Value == authtoken);


            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                // This should ideally not happen if [Authorize] is working correctly,
                // but it's good practice to handle it.
                return Unauthorized("Could not determine user identity.");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return BadRequest("The user associated with this action does not exist.");
            }

            string userFolderPath = UserPath(userName);

            // Sanitize the filename to prevent path traversal attacks.
            var fileName = Path.GetFileName(file1.FileName);
            var storedFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(userFolderPath, storedFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file1.CopyToAsync(stream);
            }


            var dxfFile = new DxfFile
            {
                FileName = fileName,
                StoredFileName = storedFileName,
                ContentType = file1.ContentType,
                UploadedAt = DateTime.UtcNow,
                AppUserId = user.Id
            };
            var referenceDrawing = new ReferenceDrawing
            { DxfFileId = dxfFile.Id,
                Name = Path.GetFileName(file2.FileName)

            };
           
            _context.DxfFiles.Add(dxfFile);
            _context.ReferenceDrawings.Add(referenceDrawing);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFiles), new { id = dxfFile.Id }, dxfFile);
        }

        private string UserPath(string userName)
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
    }
}
