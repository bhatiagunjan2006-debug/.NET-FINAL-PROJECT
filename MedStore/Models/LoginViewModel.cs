using System.ComponentModel.DataAnnotations;

namespace MedStore.Models;
public sealed class LoginViewModel
{
    [Required, StringLength(80)] public string Username { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
