using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Services.Implements.VNPayService.Models
{
    public class PaymentInformationModel
    {
        public Guid ClaimId { get; set; }
        public decimal Amount { get; set; }
        public string? ClaimType { get; set; }
    }

}
