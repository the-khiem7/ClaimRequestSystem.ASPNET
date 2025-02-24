using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ReturnClaimResponse
    {
        public Guid Id { get; set; }
        public string Remark { get; set; }
        public ClaimStatus Status { get; set; }
        public Guid ApproverId { get; set; }
        public DateTime UpdateAt { get; set; }
    }
}