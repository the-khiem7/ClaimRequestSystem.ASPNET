using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.API.Data.Entities
{
    public enum Position
    {
        ProjectManager,
        Staff,
        Finance,
        Admin
    }

    [Table("Staffs")]
    public class Staff
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Required]
        [Column("name")]
        public string Name { get; set; }
        [Required]
        [Column("email")]
        public string Email { get; set; }
        [Required]
        [Column("password")]
        public string Password { get; set; }
        [Required]
        [Column("role")]
        public Position Position { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Claim> Claims { get; set; } = [];

        public virtual ICollection<ProjectStaff> ProjectStaffs { get; set; } = [];


    }
}
