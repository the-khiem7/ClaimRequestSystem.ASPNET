using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum ProjectRole
    {
        ProjectManager,
        Developer,
        Tester,
        BusinessAnalyst,
        QualityAssurance,
        Manager,
        Finance
    }

    [Table("ProjectStaffs")]
    public class ProjectStaff
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Project))]
        [Column("project_id")]
        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [Required]
        [ForeignKey(nameof(Staff))]
        [Column("staff_id")]
        public Guid StaffId { get; set; }
        public virtual Staff Staff { get; set; }

        [Required]
        [Column("role")]
        public ProjectRole ProjectRole { get; set; }
    }
}
