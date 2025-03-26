namespace ClaimRequest.DAL.Data.Responses.Staff
{
    // tao class response de tra thong tin ve cho client
    public class CreateStaffResponse
    {
        public Guid Id { get; set; }

        public string ResponseName { get; set; }

        public string Email { get; set; }

        public string SystemRole { get; set; }

        public string Department { get; set; }

        public decimal Salary { get; set; }

        public bool IsActive { get; set; }
        public string? Avatar { get; set; }
    }
}
