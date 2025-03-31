using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Entities
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        [Required]
        [Column("payment_id")]
        public Guid PaymentId { get; set; }

        [Required]
        [Column("claim_id")]
        public Guid ClaimId { get; set; }

        [ForeignKey(nameof(ClaimId))]
        public virtual Claim Claim { get; set; } = null!;
    }
}
