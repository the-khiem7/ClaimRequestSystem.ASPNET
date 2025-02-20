using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{


    public class PaidClaimRequest
    {
        public Guid ClaimId;
        public object PaidBy;
        public string Remark;

        public Guid ApproverId { get; set; }
        public string PaymentReference { get; set; }
    }

}