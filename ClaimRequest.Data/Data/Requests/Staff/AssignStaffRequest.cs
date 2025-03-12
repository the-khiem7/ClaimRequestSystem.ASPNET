using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Requests.Staff
{
    public class AssignStaffRequest
    {
        public Guid projectId { get; set; }
        public Guid AssignerId { get; set; }
        public ProjectRole ProjectRole { get; set; }
    }
}
