using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum ClaimStatus
    {
        Draft,
        Submit,
        Approve,
        Reject,
        Return,
        Paid
    }

    [Table("Claims")]
    public class Claim
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("claim_type")]
        public string ClaimType { get; set; }

        [Required]
        [Column("status")]
        public ClaimStatus Status { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("amount")]
        [Required]
        public decimal Amount { get; set; }

        [Column("create_at", TypeName = "timestamp with time zone")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [Column("update_at", TypeName = "timestamp with time zone")]
        [DataType(DataType.DateTime)]
        public DateTime UpdateAt { get; set; }

        // One Claim belongs to one Project
        [ForeignKey(nameof(Project))]
        [Column("project_id")]
        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; }

        // One Claim has one Claimer
        [ForeignKey(nameof(Claimer))]
        [Column("claimer_id")]
        public Guid ClaimerId { get; set; }
        public virtual Staff Claimer { get; set; }

        // One Claim has many Approvers via the explicit join entity
        public virtual ICollection<ClaimApprover> ClaimApprovers { get; set; } = new List<ClaimApprover>();

        // One Claim has one Finance (last approver)
        [ForeignKey(nameof(Finance))]
        [Column("finance_id")]
        public Guid FinanceId { get; set; }
        public virtual Staff Finance { get; set; }
    }
}
