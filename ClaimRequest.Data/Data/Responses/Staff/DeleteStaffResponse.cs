namespace ClaimRequest.DAL.Data.Responses.Staff
{
    public class DeleteStaffResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public DeleteStaffResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
