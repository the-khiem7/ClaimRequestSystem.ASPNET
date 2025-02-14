using ClaimRequest.DAL.Data.Entities;
using System.ComponentModel.DataAnnotations;

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
        public SystemRole SystemRole { get; set; }

        [Required(ErrorMessage = "Department field is required")]
        public Department Department { get; set; }

        [Required(ErrorMessage = "Salary field is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be greater than 0")]
        public decimal Salary { get; set; }
    }
}