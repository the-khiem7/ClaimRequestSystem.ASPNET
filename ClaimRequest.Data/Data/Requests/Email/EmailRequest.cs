using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Email
{
    public class SendMailRequest
    {
        [Required(ErrorMessage = "Recipient email is required")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Body is required")]
        public string Body { get; set; } = string.Empty;

        //public List<string>? Attachments { get; set; }
    }
}
