using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class RejectClaimRequest
    {
        [Required]
        public Guid Id { get; set; }    //Id của Claim bị từ chối

        [Required]
        [StringLength(1000, ErrorMessage = "Remark cannot exceed 1000 characters.")]
        public string Remark { get; set; } // Lý do từ chối

        [Required]
        public Guid ApproverId { get; set; }    //Id của người từ chối
    }
}
