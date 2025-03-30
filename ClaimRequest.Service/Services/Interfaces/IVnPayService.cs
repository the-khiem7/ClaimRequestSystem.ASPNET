using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using Microsoft.AspNetCore.Http;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
