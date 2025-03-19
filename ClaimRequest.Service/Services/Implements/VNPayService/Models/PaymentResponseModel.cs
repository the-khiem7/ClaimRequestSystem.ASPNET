using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Services.Implements.VNPayService.Models
{
    public class PaymentResponseModel
    {
        public string? PaymentId { get; set; }
        public bool Success { get; set; }
        public string? VnPayResponseCode { get; set; }
    }
}
