using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Auth
{
    public class ChangePasswordResponse
    {
        public bool Success { get; set; }
        public int AttemptsLeft { get; set; }
        public string Message { get; set; }
    }
}
