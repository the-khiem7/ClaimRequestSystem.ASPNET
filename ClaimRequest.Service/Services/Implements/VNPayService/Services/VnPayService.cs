using ClaimRequest.BLL.Services.Implements.VNPayService.Library;
using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
            var timeZoneId = _configuration["TimeZoneId"];
            if (string.IsNullOrEmpty(timeZoneId))
            {
                throw new ArgumentNullException("TimeZoneId is not configured in appsettings.json");
            }

            // Kiểm tra TimeZone có hợp lệ không
            TimeZoneInfo timeZoneById;
            try
            {
                timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new Exception($"TimeZoneId '{timeZoneId}' is invalid. Check available time zones on the server.");
            }

            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var pay = new VnPayLibrary();

            var uniqueTxnRef = $"{model.FinanceId.ToString().Replace("-", "")}{timeNow:yyyyMMddHHmmss}";

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]!);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]!);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]!);
            pay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]!);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]!);

            // Chuyển danh sách ClaimIds thành chuỗi JSON để truyền vào OrderInfo
            var orderInfoJson = JsonSerializer.Serialize(new
            {
                ClaimIds = model.ClaimIds,
                FinanceId = model.FinanceId,
                Amount = model.Amount
            });

            pay.AddRequestData("vnp_OrderInfo", orderInfoJson);
            pay.AddRequestData("vnp_OrderType", _configuration["Vnpay:OrderType"]!);
            pay.AddRequestData("vnp_TxnRef", uniqueTxnRef);

            var scheme = context.Request.Scheme;
            var host = context.Request.Host.Value;
            var returnUrl = $"{scheme}://{host}/api/v1/payment/payment-callback";
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);

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
