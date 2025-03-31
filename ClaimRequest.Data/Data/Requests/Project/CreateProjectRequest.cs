using System.ComponentModel.DataAnnotations;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Requests.Project
{
    public class CreateProjectRequest
    {
        [Required(ErrorMessage = "Project Name is required")]
        [MaxLength(256, ErrorMessage = "Project Name must not exceed 256 characters")]
        public string Name { get; set; }

        [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "StartDate is required")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Budget is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Budget must be greater than or equal to 0")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "Project Manager is required")]
        public Guid ProjectManagerId { get; set; }

        [Required(ErrorMessage = "Finance Staff is required")]
        public Guid FinanceStaffId { get; set; }

        [Required(ErrorMessage = "Project Status is required")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    }
}
