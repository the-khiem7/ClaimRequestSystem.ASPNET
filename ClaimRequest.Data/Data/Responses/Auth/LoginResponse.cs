using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public SystemRole Role { get; set; }
        public Department Department { get; set; }
        public string? avatarUrl { get; set; }
        public bool IsPasswordExpired { get; set; }
        public string RefreshToken { get; set; }

        public LoginResponse(Entities.Staff staff)
        {
            Id = staff.Id;
            Email = staff.Email;
            FullName = staff.Name;
            Role = staff.SystemRole;
            Department = staff.Department;
            avatarUrl = staff.Avatar;
        }
    }
}
