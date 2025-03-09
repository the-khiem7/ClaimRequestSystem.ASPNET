using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Staff
{
    public class RemoveStaffRequest
    {
        public Guid projectId { get; set; }
    }
}
