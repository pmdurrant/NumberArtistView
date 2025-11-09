using System;

namespace Core.Business.Objects
{
    public class DxfFile
    {
        public DxfFile()
        {
            UploadedAt = DateTime.UtcNow;
            Id = Guid.NewGuid().GetHashCode();
        } 
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string StoredFileName { get; set; } // To avoid name conflicts
        public DateTime UploadedAt { get; set; }

        // Foreign key for the user
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
