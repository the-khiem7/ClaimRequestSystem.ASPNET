using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class ReturnClaimRequest
    {
        [Required]
        [StringLength(1000, ErrorMessage = "Remark cannot exceed 1000 characters.")]
        public string Remark { get; set; }

        [Required]
        public Guid ApproverId { get; set; }
    }
}