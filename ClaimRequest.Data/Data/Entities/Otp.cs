using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Entities
{
    public class Otp
    {
        [Required]
        [Column("id")]
        public Guid Id { get; set; }
        [Required]
        [Column("email")]
        public string Email { get; set; }
        [Required]
        [Column("otpcode")]
        public string OtpCode { get; set; }
        [Required]
        [Column("expirationtime")]
        public DateTime ExpirationTime { get; set; }
        [Required]
        [Column("attemptleft")]
        public int AttemptLeft { get; set; } = 3; 
    }
}
