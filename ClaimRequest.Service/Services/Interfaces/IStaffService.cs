using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Data.Responses.Staff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IStaffService
    {
        // CRUD methods for Staff
        Task<CreateStaffResponse> CreateStaff(CreateStaffRequest createStaffRequest);
    }
}
