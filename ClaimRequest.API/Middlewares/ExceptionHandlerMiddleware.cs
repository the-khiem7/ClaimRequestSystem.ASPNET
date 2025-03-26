using System.Net;
using System.Security;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
            var (statusCode, message, reason, additionalData) = exception switch
            {
                #region 400 Bad Request
                ValidationException validationEx =>
                    (HttpStatusCode.BadRequest, "Validation failed", validationEx.Message, null),
                BusinessException businessEx =>
                    (HttpStatusCode.BadRequest, "Business rule violation", businessEx.Message, null),
                InvalidOperationException =>
                    (HttpStatusCode.BadRequest, "Invalid operation", exception.Message, null),
                ArgumentException =>
                    (HttpStatusCode.BadRequest, "Invalid argument", exception.Message, null),
                #endregion

                #region 401 Unauthorized
                UnauthorizedAccessException =>
                    (HttpStatusCode.Unauthorized, "Unauthorized access", "You don't have permission to perform this action", null),
                SecurityTokenException =>
                    (HttpStatusCode.Unauthorized, "Invalid token", "Authentication token is invalid or expired", null),
                WrongPasswordException =>
                    (HttpStatusCode.Unauthorized, "Wrong password", "Invalid password. Please try again.", null),

                #endregion

                #region 403 Forbidden
                SecurityException =>
                    (HttpStatusCode.Forbidden, "Access forbidden", "You don't have sufficient permissions", null),
                PasswordExpiredException passwordExpiredEx =>
                    (HttpStatusCode.Forbidden, "Login Suspended", "Your password has expired. Please reset your password",
                     new { ResetToken = passwordExpiredEx.Message }),
                #endregion

                #region 404 Not Found
                NotFoundException =>
                    (HttpStatusCode.NotFound, "Resource not found", exception.Message, null),
                KeyNotFoundException =>
                    (HttpStatusCode.NotFound, "Resource not found", exception.Message, null),
                #endregion

                #region 409 Conflict
                EmailAlreadyRegisteredException =>
                    (HttpStatusCode.Conflict, "Email already registed, please use another email", exception.Message, null),
                #endregion

                #region 500 Internal Server Error
            DbUpdateException dbUpdateEx =>
                    (HttpStatusCode.InternalServerError, "Database update error", dbUpdateEx.Message, null),
                _ => (HttpStatusCode.InternalServerError,
                    "An unexpected error occurred",
                    _env.IsDevelopment() ? exception.Message : "Internal server error",
                    null)
                #endregion
            };

            var response = new ApiResponse<object>
            {
                StatusCode = (int)statusCode,
                Message = message,
                Reason = reason,
                IsSuccess = false,
                Data = new
                {
                    ResetToken = exception is PasswordExpiredException expiredException ? expiredException.ResetToken : null,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
                    InnerException = exception.InnerException?.Message,
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

