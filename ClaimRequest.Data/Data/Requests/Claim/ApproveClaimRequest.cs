using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class ApproveClaimRequest
    {
        [Required]
        [StringLength(500, ErrorMessage = "Remark cannot exceed 500 characters.")]
        public string Remark { get; set; }
        [Required]
        public Guid ApproverId { get; set; }
    }
}
