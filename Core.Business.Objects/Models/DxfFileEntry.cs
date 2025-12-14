using SQLite;

namespace Core.Business.Objects
{
    public class DxfFileEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string ResourceName { get; set; }
        public Guid AppUserId { get; set; }

        // Use long for SQLite "bigint" compatibility
        public long ReferenceDrawingId { get; set; }
    }
}