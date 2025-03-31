namespace ClaimRequest.DAL.Data.Responses.Auth
{
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }
        public int AttemptsLeft { get; set; }
    }
}
