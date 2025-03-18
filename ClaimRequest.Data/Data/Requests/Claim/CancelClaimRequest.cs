using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class CancelClaimRequest
    {
        [StringLength(1000, ErrorMessage = "Remark cannot exceed 1000 characters")]
        public string Remark { get; set; }
    }
}
