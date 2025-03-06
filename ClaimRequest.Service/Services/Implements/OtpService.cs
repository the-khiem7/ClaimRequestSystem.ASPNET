using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Responses.Otp;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.BLL.Services.Implements
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork<ClaimRequestDbContext> _unitOfWork;

        public OtpService(IUnitOfWork<ClaimRequestDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateOtpEntity(string email, string otp)
        {
            var otpRepository = _unitOfWork.GetRepository<Otp>();

            // Delete existing OTP for the email
            var existingOtp = await otpRepository.SingleOrDefaultAsync(predicate: o => o.Email == email);
            if (existingOtp != null)
            {
                otpRepository.DeleteAsync(existingOtp);
                await _unitOfWork.CommitAsync();
            }

            var otpEntity = new Otp
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otp,
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                AttemptLeft = 3
            };

            await otpRepository.InsertAsync(otpEntity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<ValidateOtpResponse> ValidateOtp(string email, string otp)
        {
            var otpRepository = _unitOfWork.GetRepository<Otp>();
            var otpEntity = await otpRepository.SingleOrDefaultAsync(predicate: o => o.Email == email);

            if (otpEntity == null)
            {
                return new ValidateOtpResponse
                {
                    Success = false,
                    Message = "No OTP exists for the provided email.",
                    AttemptsLeft = 0
                };
            }

            if (otpEntity.ExpirationTime < DateTime.UtcNow)
            {
                otpRepository.DeleteAsync(otpEntity);
                await _unitOfWork.CommitAsync();
                return new ValidateOtpResponse
                {
                    Success = false,
                    Message = "OTP has expired.",
                    AttemptsLeft = 0
                };
            }

            if (otpEntity.OtpCode != otp)
            {
                otpEntity.AttemptLeft--;

                if (otpEntity.AttemptLeft <= 0)
                {
                    otpRepository.DeleteAsync(otpEntity);
                }
                else
                {
                    otpRepository.UpdateAsync(otpEntity);
                }

                await _unitOfWork.CommitAsync();

                return new ValidateOtpResponse
                {
                    Success = false,
                    Message = "Invalid OTP.",
                    AttemptsLeft = otpEntity.AttemptLeft
                };
            }

            // OTP is valid, remove it
            otpRepository.DeleteAsync(otpEntity);
            await _unitOfWork.CommitAsync();

            return new ValidateOtpResponse
            {
                Success = true,
                Message = "OTP is valid.",
                AttemptsLeft = otpEntity.AttemptLeft
            };
        }
    }
}
