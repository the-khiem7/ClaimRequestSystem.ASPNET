using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.API.Data.Entities
{
    public class Claim
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ClaimType { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public string CreateAt { get; set; }
        public string UpdateAt { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [ForeignKey("Staff")]
        public int StaffId { get; set; }
        public virtual Staff Staff { get; set; }

        [ForeignKey("Approver")]
        public int ApproverId { get; set; }
        public virtual Staff Approver { get; set; }

        [ForeignKey("Finance")]
        public int FinanceId { get; set; }
        public virtual Staff Finance { get; set; }

    }
}
