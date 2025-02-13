using ClaimRequest.DAL.Data.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Data.Responses.Staff
{
    public class CreateStaffResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public SystemRole SystemRole { get; set; }

        public Department Department { get; set; }

        public decimal Salary { get; set; }

        public bool IsActive { get; set; }
    }
}
