namespace ClaimRequest.API.Constants
{
    public class ApiEndPointConstant
    {
        static ApiEndPointConstant()
        { }

        public const string RootEndpoint = "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndpoint + ApiVersion;

        public static class Claim
        {
            public const string ClaimsEndpoint = ApiEndpoint + "/claims";
            public const string ClaimEndpointById = ClaimsEndpoint + "/{id}";
            public const string UpdateClaimEndpoint = ClaimEndpointById + "/update";
            public const string CancelClaimEndpoint = ClaimsEndpoint + "/cancel";
            public const string RejectClaimEndpoint = ClaimEndpointById + "/reject";
            public const string ApproveClaimEndpoint = ClaimEndpointById + "/approve";
            public const string DownloadClaimEndpoint = ClaimsEndpoint + "/download";
            public const string PaidClaimEndpoint = ClaimEndpointById + "/paid";
        }
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
        }
        public static class Email
        {
            public const string EmailEndpoint = ApiEndpoint + "/email";
            public const string SendEmail = EmailEndpoint + "/send";
        }
    }
}
