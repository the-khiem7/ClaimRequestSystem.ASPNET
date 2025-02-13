using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum ClaimStatus
    {
        Draft,
        Pending,
        Approved,
        Paid,
        Rejected,
        Cancelled
    }

    public enum ClaimType
    {
        HardwareRequest,        // Requesting laptops, monitors, or peripherals
        SoftwareLicense,        // Claiming a license or software subscription
        OvertimeCompensation,   // Claiming extra payment for overtime work
        ProjectBudgetIncrease,  // Requesting additional budget for a project
        EquipmentRepair,        // Claiming repairs or replacements for broken equipment
        Miscellaneous           // Any other claim that doesn't fit predefined types
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
        public ClaimType ClaimType { get; set; }

        [Required]
        [Column("status")]
        public ClaimStatus Status { get; set; }

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Column("remark")]
        [StringLength(1000)]
        public string Remark { get; set; }

        [Column("amount", TypeName = "numeric")]
        [Required]
        public decimal Amount { get; set; }

        [Column("create_at", TypeName = "timestamp with time zone")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [Column("update_at", TypeName = "timestamp with time zone")]
        [DataType(DataType.DateTime)]
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;

        [Column("total_working_hours", TypeName = "numeric")]
        [Required]
        public decimal TotalWorkingHours { get; set; }

        [Column("start_date", TypeName = "date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Column("end_date", TypeName = "date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

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
        public virtual ICollection<ClaimApprover>? ClaimApprovers { get; set; } = new List<ClaimApprover>();

        // One Claim has one Finance (last approver)
        [ForeignKey(nameof(Finance))]
        [Column("finance_id")]
        public Guid? FinanceId { get; set; }
        public virtual Staff? Finance { get; set; }

        // Add navigation property for change logs
        public virtual ICollection<ClaimChangeLog> ChangeHistory { get; set; } = new List<ClaimChangeLog>();
    }
}
