using System.ComponentModel.DataAnnotations;

namespace test.Models;

public class UserViewModel
{
    public string? Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string? Password { get; set; }

    public string? Role { get; set; }
}
