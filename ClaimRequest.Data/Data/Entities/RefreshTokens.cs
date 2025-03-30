using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    [Table("RefreshTokens")]
    public class RefreshTokens
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("token")]
        public string Token { get; set; }

        [Required]
        [Column("create_at")]
        public DateTime CreateAt { get; set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [ForeignKey("UserId")]
        public virtual Staff Staff { get; set; }
    }
}
