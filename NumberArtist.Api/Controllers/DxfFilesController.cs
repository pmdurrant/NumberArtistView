using Core.Business.Objects;
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
        private readonly ApplicationDbContext _context;

        public DxfFilesController(IWebHostEnvironment env, ApplicationDbContext context)
        {
            _filesPath = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "dxf_files");

            // Ensure the directory exists.
            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath);
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

            if (Path.GetExtension(file.FileName)?.ToLowerInvariant() != ".dxf")
            {
                return BadRequest("Only .dxf files are allowed.");
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


            var userFolderPath = Path.Combine(_filesPath, userName);
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

            _context.DxfFiles.Add(dxfFile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFiles), new { id = dxfFile.Id }, dxfFile);
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
