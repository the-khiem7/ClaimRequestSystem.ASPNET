using System.ComponentModel.DataAnnotations;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class UpdateClaimRequest
    {
        [Required(ErrorMessage = "Claim Type is required")]
        public ClaimType ClaimType { get; set; }

        [Required(ErrorMessage = "Claim Name is required")]
        [MaxLength(100, ErrorMessage = "Claim Name cannot be more than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Remark Description is required")]
        [MaxLength(500, ErrorMessage = "Remark Description cannot be more than 500 characters")]
        public string Remark { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required")]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Total Working Hours is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total Working Hours must be a positive number")]
        public decimal TotalWorkingHours { get; set; }
    }

}
