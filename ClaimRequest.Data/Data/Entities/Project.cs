using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum ProjectStatus
    {
        Draft,
        Ongoing,
        Rejected,
        Archived
    }

    [Table("Projects")]
    public class Project
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(256)]
        public string Name { get; set; }

        [Column("description")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Column("status")]
        public ProjectStatus Status { get; set; }

        [Column("start_date")]
        [Required]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("end_date")]
        [Required]
        public DateOnly? EndDate { get; set; }

        [Column("budget", TypeName = "numeric")]
        public decimal Budget { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [ForeignKey("ProjectManager")]
        [Column("project_manager_id")]
        public Guid ProjectManagerId { get; set; }
        public virtual Staff ProjectManager { get; set; }

        [Required]
        [ForeignKey("FinanceStaff")]
        [Column("finance_id")]
        public Guid FinanceStaffId { get; set; }

        public virtual Staff FinanceStaff { get; set; }


        public virtual ICollection<Claim>? Claims { get; set; } = new List<Claim>();

        public virtual ICollection<ProjectStaff>? ProjectStaffs { get; set; } = new List<ProjectStaff>();
    }
}