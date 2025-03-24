using ClaimRequest.BLL.Services.Implements.VNPayService.Library;
using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ClaimRequest.BLL.Services.Implements.VNPayService.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]!);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var pay = new VnPayLibrary();

            var uniqueTxnRef = $"{model.ClaimId.ToString().Replace("-", "")}{timeNow.ToString("yyyyMMddHHmmss")}";

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]!);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]!);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]!);
            pay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]!);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]!);
            pay.AddRequestData("vnp_OrderInfo", $"ClaimType={model.ClaimType}|FinanceId={model.FinanceId}|Amount={model.Amount}".Replace(" ", ""));
            pay.AddRequestData("vnp_OrderType", _configuration["Vnpay:OrderType"]!);
            pay.AddRequestData("vnp_TxnRef", uniqueTxnRef);

            var scheme = context.Request.Scheme;
            var host = context.Request.Host.Value;

            // Redirect URL for user after payment (handled on frontend)
            var returnUrl = $"{scheme}://{host}/api/v1/payment/payment-callback"; // <-- Change this to your frontend route
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);

            //// Instant Payment Notification URL (auto backend callback)
            //var ipnUrl = $"{scheme}://{host}/api/v1/payment/payment-callback";
            //pay.AddRequestData("vnp_IpnUrl", ipnUrl);

            var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"]!, _configuration["Vnpay:HashSecret"]!);
            Console.WriteLine("Generated VNPay URL: " + paymentUrl);
            return paymentUrl;
        }


        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]!);
            return response;
        }
    }
}
