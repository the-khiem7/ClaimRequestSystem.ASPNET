using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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



        [Required(ErrorMessage = "Total working hour is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Must be greater than 0")]
        public DateTime StartDate { get; set; }


        [Required(ErrorMessage = "Total working hour is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Must be greater than 0")]
        public DateTime EndDate { get; set; }


        [Required(ErrorMessage = "Project Id is required")]
        public Guid ProjectId { get; set; }



        [Required(ErrorMessage = "Claimer Id is required")]
        public Guid ClaimerId { get; set; }
    }
}
