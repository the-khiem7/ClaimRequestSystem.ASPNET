using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Staff
{
    public class DeleteStaffResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public DeleteStaffResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
