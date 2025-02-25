using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    [Table("ClaimApprovers")]
    public class ClaimApprover
    {

        // Foreign key to Claim
        [Required]
        [Column("claim_id")]
        public Guid ClaimId { get; set; }
        [ForeignKey(nameof(ClaimId))]
        public virtual Claim Claim { get; set; }

        // Foreign key to Staff (Approver)
        [Required]
        [Column("approver_id")]
        public Guid ApproverId { get; set; }
        [ForeignKey(nameof(ApproverId))]
        public virtual Staff Approver { get; set; }
    }
}
