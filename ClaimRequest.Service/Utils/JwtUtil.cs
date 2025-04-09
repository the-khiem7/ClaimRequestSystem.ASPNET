using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Claim = System.Security.Claims.Claim;

namespace ClaimRequest.BLL.Utils
{
    public class JwtUtil : IJwtUtil
    {
        private readonly string _jwtkey, _issuer, _audience;
        private readonly double _expired;
        public JwtUtil(IConfiguration configuration)
        {
            _jwtkey = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
            _expired = double.Parse(configuration["Jwt:TokenValidityInMinutes"]);
        }

        public string GenerateJwtToken(Staff staff, Tuple<string, Guid> guidClaimer, bool isResetPasswordOnly)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SymmetricSecurityKey secrectKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtkey));
            var credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
            string issuer = _issuer;

            List<Claim> securityClaims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, staff.Email.ToString()),
                new Claim(ClaimTypes.Role, staff.SystemRole.ToString()),
            };

            if (guidClaimer != null)
                securityClaims.Add(new Claim(guidClaimer.Item1, guidClaimer.Item2.ToString()));

            // Nếu mật khẩu hết hạn, thêm claim đặc biệt
            if (isResetPasswordOnly)
            {
                securityClaims.Add(new Claim("ResetPasswordOnly", "true"));
            }

            var expires = DateTime.Now.AddMinutes(_expired);
            var token = new JwtSecurityToken(issuer, _audience, securityClaims, DateTime.Now, expires, credentials);

            return tokenHandler.WriteToken(token);
        }
    }
}
