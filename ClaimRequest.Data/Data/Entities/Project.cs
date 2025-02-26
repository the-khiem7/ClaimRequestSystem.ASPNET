using System;
using System.Collections.Generic;
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
        [StringLength(100)]
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
        [ForeignKey("ProjectManager")]
        [Column("project_manager_id")]
        public Guid ProjectManagerId { get; set; }
        public virtual Staff ProjectManager { get; set; }

        public virtual ICollection<Claim>? Claims { get; set; } = new List<Claim>();

        public virtual ICollection<ProjectStaff>? ProjectStaffs { get; set; } = new List<ProjectStaff>();
    }
}