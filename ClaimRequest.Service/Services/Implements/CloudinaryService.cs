using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace ClaimRequest.BLL.Services.Implements
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
        {
            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Cloudinary credentials are not configured properly.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string token)
        {
            try
            {
                if (imageStream == null || string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Invalid image file.");
                }

                var staffId = ExtractStaffIdFromToken(token);
                if (string.IsNullOrEmpty(staffId))
                {
                    throw new UnauthorizedAccessException("Invalid or missing StaffId in token.");
                }

                var publicId = $"images/{staffId}";

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, imageStream),
                    PublicId = publicId,
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary upload error: {ex.Message} | StackTrace: {ex.StackTrace}");
                return $"Error: {ex.Message}"; 
            }

        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> UpdateImageAsync(Stream imageStream, string fileName, string publicId)
        {
            try
            {
                await DeleteImageAsync(publicId);
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, imageStream),
                    PublicId = publicId,
                    Overwrite = true,
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary update error: {ex.Message}");
                throw new Exception("Failed to update image.");
            }
        }
        private string ExtractStaffIdFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token is missing.");
                return null;
            }
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                var staffClaim = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "StaffId");
                return staffClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting StaffId from token: {ex.Message} | StackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}
