using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.API.Data.Entities
{
    public enum ProjectRole
    {
        ProjectManager,
        Member
    }

    [Table("ProjectStaffs")]
    public class ProjectStaff
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Project))]
        [Column("project_id")]
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [Required]
        [ForeignKey(nameof(Staff))]
        [Column("staff_id")]
        public int StaffId { get; set; }
        public virtual Staff Staff { get; set; }

        [Required]
        [Column("role")]
        public ProjectRole ProjectRole { get; set; }
    }
}
