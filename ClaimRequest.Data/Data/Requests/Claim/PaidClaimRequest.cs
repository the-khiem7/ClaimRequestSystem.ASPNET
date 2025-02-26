using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class PaidClaimRequest
    {
        public DateTime PaidDate { get; set; } // Ngày thanh toán
        public decimal PaidAmount { get; set; } // Số tiền thanh toán
        public string? Note { get; set; } // Ghi chú (nếu có)
    }

}
