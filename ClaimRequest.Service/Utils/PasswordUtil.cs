using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace ClaimRequest.BLL.Utils
{
    public static class PasswordUtil
    {
        private const int WorkFactor = 12;
        public static async Task<string> HashPassword(string rawPassword)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor : WorkFactor));
        }

        public static async Task<bool> VerifyPassword(string rawPassword, string hashedPassword)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(rawPassword, hashedPassword));
        }
    }
}
