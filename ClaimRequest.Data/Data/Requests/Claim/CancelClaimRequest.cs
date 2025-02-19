using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class CancelClaimRequest
    {
        [Required(ErrorMessage = "Claim Id is required")]
        public Guid ClaimId { get; set; }

        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }

        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}
