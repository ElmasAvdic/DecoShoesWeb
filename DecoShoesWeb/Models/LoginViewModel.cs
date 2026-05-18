using System.ComponentModel.DataAnnotations;

namespace DecoShoesWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesi ispravan email.")]
        public string Email { get; set; } = string.Empty;
    }
}
