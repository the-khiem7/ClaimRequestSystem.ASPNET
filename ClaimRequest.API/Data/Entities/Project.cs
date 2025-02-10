using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.API.Data.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public string Status { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public long Budget { get; set; }

        [Required]
        [ForeignKey("ProjectManager")]
        public int ProjectManagerId { get; set; }
        public virtual Staff ProjectManager { get; set; }

        public virtual ICollection<Claim>? Claims { get; set; }
        public virtual ICollection<Staff>? Staffs { get; set; }


    }
}
