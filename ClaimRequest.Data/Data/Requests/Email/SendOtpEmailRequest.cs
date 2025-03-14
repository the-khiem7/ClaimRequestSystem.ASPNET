using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Email
{
    public class SendOtpEmailRequest
    {
        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}
