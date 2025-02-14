using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Staff
{
    // tao class CreateStaffRequest de nhan thong tin tu client gui ve BE
    public class CreateStaffRequest
    {

        [Required(ErrorMessage = "Name field is required")]
        [MaxLength(100, ErrorMessage = "Name cannot be more than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email field is required")]
        [MaxLength(256, ErrorMessage = "Email cannot be more than 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password field is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role field is required")]
        public SystemRole SystemRole { get; set; }

        [Required(ErrorMessage = "Department field is required")]
        public Department Department { get; set; }

        [Required(ErrorMessage = "Salary field is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be greater than 0")]
        public decimal Salary { get; set; }

    }
}
