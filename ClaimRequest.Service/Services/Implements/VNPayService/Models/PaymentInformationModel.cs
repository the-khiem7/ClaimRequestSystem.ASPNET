namespace ClaimRequest.BLL.Services.Implements.VNPayService.Models
{
    public class PaymentInformationModel
    {
        public Guid FinanceId { get; set; }
        public List<Guid> ClaimIds { get; set; } = new List<Guid>();
        public decimal Amount { get; set; }
        public string? ClaimType { get; set; }
    }

}
