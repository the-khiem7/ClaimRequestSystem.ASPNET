using System.Security.Cryptography;
using System.Text;
using OtpNet;

namespace ClaimRequest.BLL.Utils
{
    public static class OtpUtil
    {
        static string GenerateSecretKey(string email)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("never_gonna")))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(email));
                return Base32Encoding.ToString(hash);
            }
        }

        public static string GenerateOtp(string email)
        {
            var secretKey = GenerateSecretKey(email);
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            var otp = totp.ComputeTotp();
            return otp;
        }
    }
}
