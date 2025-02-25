using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Exceptions;
using System.Net;

namespace ClaimRequest.API.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        // Fields
        private readonly ILogger<ExceptionHandlerMiddleware> _logger; // for logging
        private readonly RequestDelegate _next; // for the next middleware
        private readonly IHostEnvironment _env; // for

        //constructor
        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _env = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var errorId = Guid.NewGuid().ToString();
                LogError(errorId, context, exception);
                await HandleExceptionAsync(context, errorId, exception);
            }
        }

        private void LogError(string errorId, HttpContext context, Exception exception)
        {
            var error = new
            {
                ErrorId = errorId,
                Timestamp = DateTime.UtcNow,
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                User = context.User?.Identity?.Name ?? "Anonymous",
                AdditionalInfo = GetAdditionalInfo(exception)
            };

            var logLevel = exception switch
            {
                BusinessException => LogLevel.Warning,
                ValidationException => LogLevel.Warning,
                NotFoundException => LogLevel.Information,
                _ => LogLevel.Error
            };

            _logger.Log(logLevel, exception,
                "Error ID: {ErrorId} - Path: {Path} - Method: {Method} - {@error}",
                errorId,
                context.Request.Path,
                context.Request.Method,
                error);
        }

        private object GetAdditionalInfo(Exception exception)
        {
            return exception switch
            {
                ValidationException valEx => new
                {
                    ValidationDetails = valEx.Message
                },
                BusinessException busEx => new
                {
                    BusinessRule = busEx.Message
                },
                _ => new { }
            };
        }

        private async Task HandleExceptionAsync(HttpContext context, string errorId, Exception exception)
        {
            var (statusCode, message, reason) = exception switch
            {
                ValidationException validationEx =>
                    (HttpStatusCode.BadRequest, "Validation failed", validationEx.Message),

                NotFoundException notFoundEx =>
                    (HttpStatusCode.NotFound, "Resource not found", notFoundEx.Message),

                BusinessException businessEx =>
                    (HttpStatusCode.BadRequest, "Business rule violation", businessEx.Message),

                UnauthorizedAccessException =>
                    (HttpStatusCode.Unauthorized, "Unauthorized access", "You don't have permission to perform this action"),

                InvalidOperationException =>
                    (HttpStatusCode.BadRequest, "Invalid operation", exception.Message),

                _ => (HttpStatusCode.InternalServerError,
                    "An unexpected error occurred",
                    _env.IsDevelopment() ? exception.Message : "Internal server error")
            };

            var response = new ApiResponse<object>
            {
                StatusCode = (int)statusCode,
                Message = message,
                Reason = reason,
                IsSuccess = false,
                Data = new
                {
                    ErrorId = errorId,
                    Timestamp = DateTime.UtcNow
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}

