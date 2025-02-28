using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class CancelClaimRequest
    {
        [Required(ErrorMessage = "Claim Id is required")]
        public Guid ClaimId { get; set; }

        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }

    }
}
