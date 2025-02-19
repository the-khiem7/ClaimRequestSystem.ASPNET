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
            // Duoi co "s" danh cho nhung tac vu Create(POST) hoac GetALL (GET)
            public const string ClaimsEndpoint = ApiEndpoint + "/claim";

            // Duoi ko "s" danh cho cac tac vu chi dinh 1 doi tuong object: GetByID (GET), Update(PUT), Delete(DELETE)
            public const string ClaimEndpoint = ClaimsEndpoint + "/{id}";
        }
    }
}
