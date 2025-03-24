namespace ClaimRequest.BLL.Services.Implements.VNPayService.Models
{
    public class PaymentResponseModel
    {
        public string? PaymentId { get; set; }
        public bool Success { get; set; }
        public string? VnPayResponseCode { get; set; }
    }
}
