namespace ClaimRequest.DAL.Data.Responses.Otp
{
    public class ValidateOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AttemptsLeft { get; set; }
    }
}
