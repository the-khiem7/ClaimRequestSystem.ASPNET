using ClaimRequest.DAL.Data.Requests.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(SendMailRequest request);
    }
}
