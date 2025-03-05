using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class CloudinaryController : BaseController<CloudinaryController>
    {
        private readonly ICloudinaryService _cloudinaryService;
        public CloudinaryController(ILogger<CloudinaryController> logger, ICloudinaryService cloudinaryService) : base(logger)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost(ApiEndPointConstant.Cloudinary.UploadImage)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var stream = file.OpenReadStream();
            var imageUrl = await _cloudinaryService.UploadImageAsync(stream, file.FileName);

            return Ok(new { imageUrl });
        }

        [HttpDelete(ApiEndPointConstant.Cloudinary.DeleteImage)]
        public async Task<IActionResult> DeleteImage([FromRoute] string publicId)
        {
            var isDeleted = await _cloudinaryService.DeleteImageAsync(publicId);
            if (!isDeleted)
                return BadRequest("Failed to delete image.");

            return Ok("Image deleted successfully.");
        }

        [HttpPut(ApiEndPointConstant.Cloudinary.UploadImage)]
        public async Task<IActionResult> UpdateImage([FromRoute] string publicId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var imageUrl = await _cloudinaryService.UpdateImageAsync(stream, file.FileName, publicId);

            return Ok(new { imageUrl });
        }
    }
}
