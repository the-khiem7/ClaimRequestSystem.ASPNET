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
        public DateTime CreateAt { get; set; }

        [Column("update_at", TypeName = "timestamp with time zone")]
        [DataType(DataType.DateTime)]
        public DateTime UpdateAt { get; set; }

        [Column("total_working_hours", TypeName = "numeric")]
        [Required]
        public decimal TotalWorkingHours { get; set; }

        [Column("start_date")]
        [Required]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        [Required]
        public DateOnly EndDate { get; set; }

        [ForeignKey(nameof(Project))]
        [Column("project_id")]
        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [ForeignKey(nameof(Claimer))]
        [Column("claimer_id")]
        public Guid ClaimerId { get; set; }
        public virtual Staff Claimer { get; set; }

        [ForeignKey(nameof(Finance))]
        [Column("finance_id")]
        public Guid? FinanceId { get; set; }
        public virtual Staff? Finance { get; set; }

        public virtual ICollection<ClaimApprover>? ClaimApprovers { get; set; }
        public virtual ICollection<ClaimChangeLog> ChangeHistory { get; set; }
    }
}
