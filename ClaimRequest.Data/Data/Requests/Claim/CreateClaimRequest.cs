using System.ComponentModel.DataAnnotations;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class CreateClaimRequest
    {
        [Required(ErrorMessage = "Claim Type is required")]
        public ClaimType ClaimType { get; set; }



        [Required(ErrorMessage = "Claim Name is required")]
        [MaxLength(100, ErrorMessage = "Claim Name cannot be more than 100 characters")]
        public string Name { get; set; }



        [Required(ErrorMessage = " Remark Description is required")]
        [MaxLength(500, ErrorMessage = "Remark Description cannot be more than 500 characters")]
        public string Remark { get; set; }



        [Required(ErrorMessage = "Amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }



        [Required(ErrorMessage = "Create time is required")]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;



        [Required(ErrorMessage = "Total working hour is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Must be greater than 0")]
        public decimal TotalWorkingHours { get; set; }



        [Required(ErrorMessage = "Start date is required")]
        public DateOnly StartDate { get; set; }


        [Required(ErrorMessage = "End date is required")]
        [CustomValidation(typeof(CreateClaimRequest), nameof(ValidateEndDate))]
        public DateOnly EndDate { get; set; }


        [Required(ErrorMessage = "Project Id is required")]
        public Guid ProjectId { get; set; }



        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }

        public static ValidationResult ValidateEndDate(DateOnly endDate, ValidationContext context)
        {
            var instance = (CreateClaimRequest)context.ObjectInstance;
            if (endDate < instance.StartDate)
            {
                return new ValidationResult("End date must be greater than or equal to start date");
            }
            return ValidationResult.Success;
        }
    }
}
