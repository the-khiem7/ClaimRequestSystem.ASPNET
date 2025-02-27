using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public SystemRole Role { get; set; }
        public Department Department { get; set; }

        public LoginResponse(ClaimRequest.DAL.Data.Entities.Staff staff)
        {
            Id = staff.Id;
            Email = staff.Email;
            FullName = staff.Name;
            Role = staff.SystemRole;
            Department = staff.Department;
        }
    }
}
