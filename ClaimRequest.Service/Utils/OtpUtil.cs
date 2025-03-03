using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace ClaimRequest.BLL.Utils
{
    public class OtpUtil
    {
        private readonly ClaimRequestDbContext _context;
        private static readonly TimeSpan OtpValidityDuration = TimeSpan.FromMinutes(5); // OTP valid for 5 minutes

        public OtpUtil(ClaimRequestDbContext context)
        {
            _context = context;
        }

        static string GenerateSecretKey(string email)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("secret_salt")))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(email));
                return Base32Encoding.ToString(hash);
            }
        }

        public async Task<string> GenerateOtp(string email)
        {
            var secretKey = GenerateSecretKey(email);
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            var otp = totp.ComputeTotp();

            // Store OTP with expiration time associated with the email
            var otpEntity = new ClaimRequest.DAL.Data.Entities.Otp
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otp,
                ExpirationTime = DateTime.UtcNow.Add(OtpValidityDuration),
                AttemptLeft = 3 // Initialize attempts left
            };

            _context.Otps.Add(otpEntity);
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> ValidateOtp(string email, string otp)
        {
            var otpEntity = await _context.Otps
                .Where(o => o.Email == email)
                .FirstOrDefaultAsync();

            if (otpEntity != null)
            {
                if (otpEntity.OtpCode == otp && DateTime.UtcNow <= otpEntity.ExpirationTime)
                {
                    _context.Otps.Remove(otpEntity);
                    await _context.SaveChangesAsync();
                    return true; // OTP is valid and within the time frame
                }
                else
                {
                    otpEntity.AttemptLeft--;
                    if (otpEntity.AttemptLeft <= 0)
                    {
                        _context.Otps.Remove(otpEntity);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return false; // OTP is invalid or expired or wrong email
        }
    }
}
