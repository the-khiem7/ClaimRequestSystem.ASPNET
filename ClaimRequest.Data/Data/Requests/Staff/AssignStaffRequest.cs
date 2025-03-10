using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Staff
{
    public class AssignStaffRequest
    {
        public Guid projectId { get; set; }
        public Guid AssignerId { get; set; }
        //public ProjectRole ProjectRole { get; set; }
    }
}
