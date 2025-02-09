using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.API.Data.Entities
{
    enum Role
    {
        ProjectManager,
        Staff,
        Finance
    }
    public class Staff
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
