using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public class ClaimChangeLog
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid HistoryId { get; set; }

        // Foreign key linking back to the Claim
        [ForeignKey(nameof(Claim))]
        [Required]
        [Column("claim_id")]
        public Guid ClaimId { get; set; }
        public virtual Claim Claim { get; set; }

        // Details about the change
        [Required]
        [Column("field_changed")]
        public string FieldChanged { get; set; }

        [Column("old_value")]
        public string OldValue { get; set; }

        [Column("new_value")]
        public string NewValue { get; set; }

        [Column("changed_at", TypeName = "timestamp with time zone")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ChangedAt { get; set; }

        // Optionally, store who made the change
        [Column("changed_by")]
        public string ChangedBy { get; set; }
    }
}
