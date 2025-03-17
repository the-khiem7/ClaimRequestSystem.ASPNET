using System.Net;

namespace ClaimRequest.DAL.Data.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : ApiException
    {
        public NotFoundException(string message)
            : base(message, HttpStatusCode.NotFound)
        {
        }
    }

    public class BadRequestException : ApiException
    {
        public BadRequestException(string message)
            : base(message, HttpStatusCode.BadRequest)
        {
        }
    }

    public class UnauthorizedException : ApiException
    {
        public UnauthorizedException(string message)
            : base(message, HttpStatusCode.Unauthorized)
        {
        }
    }
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    public class PasswordExpiredException : ApiException
    {
        public PasswordExpiredException(string message)
            : base(message, HttpStatusCode.Forbidden)
        {
        }
    }
    public class OtpValidationException : Exception
    {
        public int AttemptsLeft { get; }

        public OtpValidationException(string message, int attemptsLeft) : base(message)
        {
            AttemptsLeft = attemptsLeft;
        }
    }
}