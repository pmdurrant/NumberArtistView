using System.ComponentModel.DataAnnotations;

namespace Core.Business.Objects
{
    public class RegisterModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}