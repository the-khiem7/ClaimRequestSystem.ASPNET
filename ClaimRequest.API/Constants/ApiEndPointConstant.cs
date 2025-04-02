namespace ClaimRequest.API.Constants
{
    public class ApiEndPointConstant
    {
        public const string RootEndpoint = "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndpoint + ApiVersion;

        public static class Navigator
        {
            public const string SideBarEndpoint = ApiEndpoint + "/sidebar";
        }
        public static class Auth
        {
            public const string AuthEndpoint = ApiEndpoint + "/auth";
            public const string LoginEndpoint = AuthEndpoint + "/login";
            public const string RegisterEndpoint = AuthEndpoint + "/register";
            public const string RefreshTokenEndpoint = AuthEndpoint + "/refresh-token";
            public const string DeleteRefreshTokenEndpoint = AuthEndpoint + "/delete-refresh-token";
            public const string LogoutEndpoint = AuthEndpoint + "/logout";
            public const string ForgotPasswordEndpoint = AuthEndpoint + "/forgot-password";
            public const string ChangePasswordEndpoint = AuthEndpoint + "/change-password";
        }

        public static class Claim
        {
            public const string ClaimsEndpoint = ApiEndpoint + "/claims";
            public const string ClaimEndpointById = ClaimsEndpoint + "/{id}";
            public const string UpdateClaimEndpoint = ClaimEndpointById + "/update";
            public const string CancelClaimEndpoint = ClaimsEndpoint + "/{claimId}" + "/cancel";
            public const string RejectClaimEndpoint = ClaimEndpointById + "/reject";
            public const string ApproveClaimEndpoint = ClaimEndpointById + "/approve";
            public const string DownloadClaimEndpoint = ClaimsEndpoint + "/download";
            public const string PaidClaimEndpoint = ClaimEndpointById + "/paid";
            public const string ReturnClaimEndpoint = ClaimEndpointById + "/return";
            public const string SubmitClaimEndpoint = ClaimEndpointById + "/submit";
        }

        public static class Email
        {
            public const string EmailEndpoint = ApiEndpoint + "/email";
            public const string SendEmail = EmailEndpoint + "/send";
            public const string SendOtp = EmailEndpoint + "/send-otp";
        }
        //public static class Otp
        //{
        //    public const string OtpEndpoint = ApiEndpoint + "/otp";
        //    public const string ValidateOtp = OtpEndpoint + "/validate";
        //}
        public static class Projects
        {
            public const string ProjectsEndpoint = ApiEndpoint + "/projects";
            public const string ProjectEndpointById = ProjectsEndpoint + "/{id}";
            public const string UpdateProjectEndpoint = ProjectEndpointById + "/update";
            public const string DeleteProjectEndpoint = ProjectEndpointById + "/delete";
        }
        public static class Staffs
        {
            public const string StaffsEndpoint = ApiEndpoint + "/staffs";
            public const string StaffEndpointById = StaffsEndpoint + "/{id}";
            public const string UpdateStaffEndpoint = StaffEndpointById + "/update";
            public const string DeleteStaffEndpoint = StaffEndpointById + "/delete";
            public const string AssignStaffEndpoint = StaffEndpointById + "/assign";
            public const string RemoveStaffEndpoint = StaffEndpointById + "/remove";
        }
        public static class Cloudinary
        {
            public const string CloudinaryEndpoint = ApiEndpoint + "/cloudinary";
            public const string UploadImage = CloudinaryEndpoint + "/upload";
            public const string UploadFile = CloudinaryEndpoint + "/upload-file";
        }

        public static class Payment
        {
            public const string PaymentControllerBase = ApiEndpoint + "/payment";
            public const string CreatePaymentUrl = PaymentControllerBase + "/create-payment-url";
            public const string PaymentCallback = PaymentControllerBase + "/payment-callback";
        }

        public static class Remnider
        {
            public const string ReminderEndpoint = ApiEndpoint + "/send-reminder";
        }

        public static class PendingReminder
        {
            public const string PendingReminderEndpoint = ApiEndpoint + "/pending-reminder";
        }

    }
}
