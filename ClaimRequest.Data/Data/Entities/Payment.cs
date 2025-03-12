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

        [Required]
        [Column("amount", TypeName = "numeric")]
        public decimal Amount { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; }

        [Column("created_at", TypeName = "timestamp with time zone")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ClaimId))]
        public virtual Claim Claim { get; set; }
    }
}
