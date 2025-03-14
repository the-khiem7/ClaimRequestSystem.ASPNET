namespace ClaimRequest.DAL.Data.Responses.Email
{
    public class SendOtpEmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
