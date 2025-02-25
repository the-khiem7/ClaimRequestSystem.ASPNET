using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Repositories.Interfaces
{
    public interface IClaimRepository
    {

        Task<Claim?> GetClaimByIdAsync(Guid claimId);
        Task<bool> IsClaimPaidAsync(Guid claimId);
        Task MarkClaimAsPaidAsync(Guid claimId, DateTime paidDate, decimal paidAmount);


    }

}
