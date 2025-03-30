using Microsoft.AspNetCore.Http;

namespace ClaimRequest.DAL.Data.Requests.Media
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
    }
}
