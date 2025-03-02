namespace ClaimRequest.BLL.Utils
{
    public static class PasswordUtil
    {
        private const int HashingRound = 10;
        public static async Task<string> HashPassword(string rawPassword)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: HashingRound));
        }

        public static async Task<bool> VerifyPassword(string rawPassword, string hashedPassword)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(rawPassword, hashedPassword));
        }
    }
}
