using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class ApproveClaimRequest
    {
        [Required]
        [StringLength(500, ErrorMessage = "Remark cannot exceed 500 characters.")]
        public string Remark { get; set; }
    }
}
