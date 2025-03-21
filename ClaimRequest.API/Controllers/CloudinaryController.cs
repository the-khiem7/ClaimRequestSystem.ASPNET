using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Requests.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    [Authorize]
    public class CloudinaryController : BaseController<CloudinaryController>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public CloudinaryController(ILogger<CloudinaryController> logger, ICloudinaryService cloudinaryService)
            : base(logger)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost(ApiEndPointConstant.Cloudinary.UploadImage)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(request.File, User);
                return Ok(new { imageUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading image: {ex}");
                return StatusCode(500, "An error occurred while uploading the image.");
            }
        }

        [HttpDelete(ApiEndPointConstant.Cloudinary.DeleteImage)]
        public async Task<IActionResult> DeleteImage([FromRoute] string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return BadRequest("Invalid public ID.");
            }

            try
            {
                var isDeleted = await _cloudinaryService.DeleteImageAsync(publicId);
                if (!isDeleted)
                {
                    return BadRequest("Failed to delete image.");
                }
                return Ok(new { message = "Image deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting image: {ex}");
                return StatusCode(500, "An error occurred while deleting the image.");
            }
        }
    }
}
