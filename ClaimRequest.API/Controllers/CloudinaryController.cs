using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class CloudinaryController : BaseController<CloudinaryController>
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<CloudinaryController> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public CloudinaryController(ILogger<CloudinaryController> logger, ICloudinaryService cloudinaryService)
            : base(logger)
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost(ApiEndPointConstant.Cloudinary.UploadImage)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(fileExtension))
                return BadRequest("Invalid file format. Allowed formats: .jpg, .jpeg, .png");

            if (file.Length > MaxFileSize)
                return BadRequest("File size exceeds the 5MB limit.");

            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Missing authentication token.");
                }

                using var stream = file.OpenReadStream();
                var imageUrl = await _cloudinaryService.UploadImageAsync(stream, file.FileName, token);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while uploading the image: {ex.Message}");
            }
        }


        [HttpDelete(ApiEndPointConstant.Cloudinary.DeleteImage)]
        public async Task<IActionResult> DeleteImage([FromRoute] string publicId)
        {
            try
            {
                var isDeleted = await _cloudinaryService.DeleteImageAsync(publicId);
                if (!isDeleted)
                    return BadRequest("Failed to delete image.");

                return Ok("Image deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting image: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the image.");
            }
        }

        [HttpPut(ApiEndPointConstant.Cloudinary.UpdateImage)]
        public async Task<IActionResult> UpdateImage([FromRoute] string publicId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(fileExtension))
                return BadRequest("Invalid file format. Allowed formats: .jpg, .jpeg, .png");

            if (file.Length > MaxFileSize)
                return BadRequest("File size exceeds the 5MB limit.");

            try
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _cloudinaryService.UpdateImageAsync(stream, file.FileName, publicId);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating image: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the image.");
            }
        }
    }
}
