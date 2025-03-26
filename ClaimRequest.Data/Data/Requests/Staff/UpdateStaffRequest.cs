using System.ComponentModel.DataAnnotations;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace ClaimRequest.DAL.Data.Requests.Staff
{
    public class UpdateStaffRequest
    {
        [Required(ErrorMessage = "Name field is required")]
        [MaxLength(100, ErrorMessage = "Name cannot be more than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email field is required")]
        [MaxLength(256, ErrorMessage = "Email cannot be more than 256 characters")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Role field is required")]
        //[StringLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
        public SystemRole SystemRole { get; set; }

        [Required(ErrorMessage = "Department field is required")]
        //[StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public Department Department { get; set; }

        [Required(ErrorMessage = "Salary field is required")]
        [Range(0, 999999999.99, ErrorMessage = "Salary must be between 0 and 999,999,999.99")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;

        public IFormFile? Avatar { get; set; }
    }
}
