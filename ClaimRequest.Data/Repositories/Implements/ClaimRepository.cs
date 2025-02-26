using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Repositories.Implements
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly ClaimRequestDbContext _context;

        public ClaimRepository(ClaimRequestDbContext context)
        {
            _context = context;
        }

        public async Task<Claim?> GetClaimByIdAsync(Guid claimId)
        {
            return await _context.Claims.FindAsync(claimId);
        }

        public async Task<bool> IsClaimPaidAsync(Guid claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            return claim != null && claim.IsPaid;
        }

        public async Task MarkClaimAsPaidAsync(Guid claimId, DateTime paidDate, decimal paidAmount)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.IsPaid = true;
                claim.PaidDate = paidDate;
                claim.PaidAmount = paidAmount;
                await _context.SaveChangesAsync();
            }
        }


    }

}
