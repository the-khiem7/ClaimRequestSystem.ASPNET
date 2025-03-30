using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum SystemRole
    {
        Approver,
        Staff,
        Finance,
        Admin
    }

    public enum Department
    {
        ProjectManagement,    // Oversees project execution
        Engineering,          // Technical teams (Software, R&D, etc.)
        FinancialDivision,    // Handles finance, budgeting, and accounting
        BusinessOperations    // Covers HR, administration, and leadership
    }


    [Table("Staffs")]
    public class Staff
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid Id { get; set; }
        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        [Column("email")]
        [StringLength(256)]
        public string Email { get; set; }
        [Required]
        [Column("password")]
        public string Password { get; set; }
        [Required]
        [Column("role")]
        public SystemRole SystemRole { get; set; }

        [Column("department")]
        [Required]
        public Department Department { get; set; }

        [Column("salary", TypeName = "numeric")]
        public decimal Salary { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("avatar")]
        public string? Avatar { get; set; }

        [Column("last_change_password")]
        public DateTime? LastChangePassword { get; set; }

        public virtual ICollection<ProjectStaff> ProjectStaffs { get; set; } = [];

        public virtual ICollection<RefreshTokens> RefreshTokens { get; set; }

        //public string PasswordHash { get; set; }
    }
}
