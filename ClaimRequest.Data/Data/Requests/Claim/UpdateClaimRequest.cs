using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class UpdateClaimRequest
    {
        [Required(ErrorMessage = "Claim Id is required")]
        public Guid ClaimId { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required")]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Total Working Hours is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total Working Hours must be a positive number")]
        public decimal TotalWorkingHours { get; set; }
    }
}
