namespace ClaimRequest.DAL.Data.Responses.Otp
{
    public class ValidateOtpResponse
    {
        public bool Success { get; set; }
        public int AttemptsLeft { get; set; }
    }
}
