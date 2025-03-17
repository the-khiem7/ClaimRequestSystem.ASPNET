using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ClaimRequest.DAL.Data.Requests.Media
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; }
    }
}
