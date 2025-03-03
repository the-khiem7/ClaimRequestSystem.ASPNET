using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ViewClaimResponse
    {
        public Guid Id { get; set; }
        public string StaffName { get; set; }
        public string ProjectName { get; set; }
        public DateOnly ProjectStartDate { get; set; }
        public DateOnly ProjectEndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }

    }
}
