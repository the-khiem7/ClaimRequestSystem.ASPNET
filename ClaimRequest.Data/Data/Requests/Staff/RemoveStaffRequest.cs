namespace ClaimRequest.DAL.Data.Requests.Staff
{
    public class RemoveStaffRequest
    {
        public Guid projectId { get; set; }

        public Guid RemoverId { get; set; }
    }
}
