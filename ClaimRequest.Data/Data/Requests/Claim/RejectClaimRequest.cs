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
        [StringLength(1000, ErrorMessage = "Remark cannot exceed 1000 characters.")]
        public string Remark { get; set; }

        [Required]
        public Guid ApproverId { get; set; }
    }
}
