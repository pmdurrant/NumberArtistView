using System.ComponentModel.DataAnnotations;

namespace Core.Business.Objects
{
    public class LoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}