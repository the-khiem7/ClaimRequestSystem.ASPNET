using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Staff
{
    public class RemoveStaffResponse
    {
        public Guid Id { get; set; }

        public Guid projectId { get; set; }

        public Guid StaffId { get; set; }

        public ProjectRole ProjectRole { get; set; }
    }
}
