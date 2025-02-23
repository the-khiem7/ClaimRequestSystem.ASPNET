using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class UpdateClaimResponse
    {
        public Guid ClaimId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
