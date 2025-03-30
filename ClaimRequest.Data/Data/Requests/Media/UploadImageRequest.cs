using Microsoft.AspNetCore.Http;

namespace ClaimRequest.DAL.Data.Requests.Media
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; }
    }
}
