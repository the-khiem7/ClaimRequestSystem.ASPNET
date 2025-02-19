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
    // define cac method CRUD cho Staff
    public interface IStaffService
    {
        // B2: Tao method CRUD cho Staff
        Task<CreateStaffResponse> CreateStaff(CreateStaffRequest createStaffRequest);
        Task<CreateStaffResponse> GetStaffById(Guid id);
        Task<IEnumerable<CreateStaffResponse>> GetStaffs();
        Task<UpdateStaffResponse> UpdateStaff(Guid id, UpdateStaffRequest updateStaffRequest);
        Task<bool> DeleteStaff(Guid id);

    }
}
