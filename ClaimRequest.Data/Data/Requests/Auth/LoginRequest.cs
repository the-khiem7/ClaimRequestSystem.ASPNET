using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }
}
