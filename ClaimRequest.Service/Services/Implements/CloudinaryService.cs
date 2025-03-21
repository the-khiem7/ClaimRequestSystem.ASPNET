using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace ClaimRequest.BLL.Services.Implements
{
    public class CloudinaryService : BaseService<CloudinaryService>, ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

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

            if (file.Length > MaxFileSize)
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

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                _logger.LogWarning("DeleteImageAsync: publicId is null or empty.");
                return false;
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok")
                {
                    _logger.LogInformation($"Successfully deleted image: {publicId}");
                    return true;
                }

                _logger.LogWarning($"Failed to delete image: {publicId}. Cloudinary response: {result.Result}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary delete error for {publicId}: {ex.Message}");
                return false;
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