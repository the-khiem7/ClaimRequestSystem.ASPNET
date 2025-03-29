using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ViewClaimByIdResponse
    {
        //Claim details
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public decimal Amount { get; set; }
        public ClaimType ClaimType { get; set; } 
        public string Remark { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

        //Claimer details
        public string StaffName { get; set; }

        //Project details
        public string ProjectName { get; set; }
        public DateOnly ProjectStartDate { get; set; }
        public DateOnly ProjectEndDate { get; set; }

        //Unknown code
        public Guid? FinanceId { get; set; }
    }
}
