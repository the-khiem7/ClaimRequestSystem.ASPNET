namespace ClaimRequest.API.Middlewares
{
    public class ResetPasswordOnlyMiddleware
    {
        private readonly RequestDelegate _next;

        public ResetPasswordOnlyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            var user = context.User;
            if (user.HasClaim(c => c.Type == "ResetPasswordOnly" && c.Value == "true"))
            {
                var path = context.Request.Path.Value.ToLower();
                if (!path.Contains("change-password"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied. This token is only valid for password reset.");
                    return;
                }
            }

            await _next(context);
        }
    }

}
