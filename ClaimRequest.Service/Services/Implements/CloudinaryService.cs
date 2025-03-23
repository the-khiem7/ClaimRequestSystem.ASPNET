using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ClaimRequest.DAL.Repositories.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using AutoMapper;
using ClaimRequest.BLL.Services.Interfaces;
using System.Text.RegularExpressions;
using System.Text;

namespace ClaimRequest.BLL.Services.Implements
{
    public class CloudinaryService : BaseService<CloudinaryService>, ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        const long MaxImageSize = 5 * 1024 * 1024;
        const long MaxFileSize = 100 * 1024 * 1024;

        public CloudinaryService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<CloudinaryService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
            : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _configuration = configuration;
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                throw new ArgumentException("Cloudinary configuration is missing");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, ClaimsPrincipal user)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Invalid file format. Allowed formats: .jpg, .jpeg, .png");

            if (file.Length > MaxImageSize)
                throw new ArgumentException("File size exceeds the 5MB limit.");

            var staffIdClaim = user.FindFirst("StaffId")?.Value;
            if (string.IsNullOrEmpty(staffIdClaim) || !Guid.TryParse(staffIdClaim, out var staffId))
                throw new UnauthorizedAccessException("Invalid token or missing staff ID.");

            var staff = await _unitOfWork.GetRepository<Staff>().GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException("Staff not found.");

            try
            {
                using var stream = file.OpenReadStream();

                var sanitizedStaffName = RemoveVietnameseAccent(staff.Name).ToLower().Replace(" ", "_");

                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                var timestamp = vietnamTime.ToString("yyyyMMddHHmmss");

                var fileName = $"{sanitizedStaffName}_{timestamp}";

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = fileName,
                    Overwrite = false,
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();

            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary upload error: {ex.Message}");
                throw new Exception("Failed to upload image.");
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, ClaimsPrincipal user)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (file.Length > MaxFileSize)
                throw new ArgumentException("File size exceeds the 100MB limit.");

            var staffIdClaim = user.FindFirst("StaffId")?.Value;
            if (string.IsNullOrEmpty(staffIdClaim) || !Guid.TryParse(staffIdClaim, out var staffId))
                throw new UnauthorizedAccessException("Invalid token or missing staff ID.");

            var staff = await _unitOfWork.GetRepository<Staff>().GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException("Staff not found.");

            try
            {
                using var stream = file.OpenReadStream();

                var sanitizedStaffName = RemoveVietnameseAccent(staff.Name).ToLower().Replace(" ", "_");

                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                var timestamp = vietnamTime.ToString("yyyyMMddHHmmss");

                var fileName = $"{sanitizedStaffName}_{timestamp}";

                var uploadParams = new AutoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = fileName,
                    Overwrite = false
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary upload error: {ex.Message}");
                throw new Exception("Failed to upload file.");
            }
        }

        private string RemoveVietnameseAccent(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Normalize(NormalizationForm.FormD);
            var regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            return regex.Replace(text, "").Replace("đ", "d").Replace("Đ", "D");
        }
    }
}