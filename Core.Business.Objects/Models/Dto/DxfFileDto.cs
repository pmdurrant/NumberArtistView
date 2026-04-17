using System.Text.Json.Serialization;

namespace Core.Business.Objects.Models.Dto
{
    public class DxfFileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("storedFileName")]
        public string StoredFileName { get; set; }
    }
}