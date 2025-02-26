using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class SubmitClaimResponse
    {
        public Guid ClaimId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal Amount { get; set; }
        public Guid ClaimerId { get; set; }
    }
}
