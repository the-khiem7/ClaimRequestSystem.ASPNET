using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    [Table("Otps")]
    public class Otp
    {
        [Key]
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
        public int AttemptLeft { get; set; }
    }
}
