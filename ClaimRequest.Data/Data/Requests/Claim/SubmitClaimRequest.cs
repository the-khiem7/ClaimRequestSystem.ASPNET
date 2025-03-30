using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class SubmitClaimRequest
    {
        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }
    }
}
