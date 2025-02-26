using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class PaidClaimResponse
    {
        public Guid ClaimId { get; set; } // ID của claim
        public DateTime PaidDate { get; set; } // Ngày thanh toán
        public decimal PaidAmount { get; set; } // Số tiền đã thanh toán
        public string? Status { get; set; } // Trạng thái sau khi thanh toán
    }

}
