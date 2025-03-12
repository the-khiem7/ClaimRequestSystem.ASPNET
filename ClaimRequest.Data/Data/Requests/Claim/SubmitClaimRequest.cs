using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class SubmitClaimRequest
    {
        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }
    }
}
