using System.ComponentModel.DataAnnotations;

namespace Core.Business.Objects.Dtos;

public class LoginModel
{
    [Required(ErrorMessage = "User Id is required")]
    public string UserId { get; set; }
    [Required(ErrorMessage = "User Name is required")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}

public class RegisterModel
{
    [Required(ErrorMessage = "User Id is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "User Name is required")]
    public string Username { get; set; }

    [EmailAddress]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}