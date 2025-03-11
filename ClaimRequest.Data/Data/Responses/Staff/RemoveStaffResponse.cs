using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Staff
{
    public class RemoveStaffResponse
    {
        public Guid Id { get; set; }

        public Guid projectId { get; set; }

        public Guid StaffId { get; set; }

        public ProjectRole ProjectRole { get; set; }
    }
}
