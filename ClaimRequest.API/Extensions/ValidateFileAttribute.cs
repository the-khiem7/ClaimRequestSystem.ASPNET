using ClaimRequest.DAL.Data.MetaDatas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClaimRequest.API.Extensions
{
    public class ValidateFileAttribute : ActionFilterAttribute
    {
        // This class is used to validate the file upload
        // If the file is not valid, it will return a bad request response
        private readonly int _maxSizeInMB;
        private readonly string[] _allowedExtensions;

        public ValidateFileAttribute(int maxSizeInMB = 5, string[] allowedExtensions = null)
        {
            _maxSizeInMB = maxSizeInMB;
            _allowedExtensions = allowedExtensions ?? new[] { ".jpg", ".jpeg", ".png" };
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var file = context.HttpContext.Request.Form.Files.FirstOrDefault();

            if (file == null)
            {
                AddError(context, "No file was uploaded");
                return;
            }

            // Validate file extension
            if (!_allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
            {
                AddError(context, $"Unsupported file extension. Only {string.Join(", ", _allowedExtensions)} are allowed");
                return;
            }

            // Validate file size
            if (file.Length > _maxSizeInMB * 1024 * 1024)
            {
                AddError(context, $"The file size must be less than {_maxSizeInMB}MB");
                return;
            }
        }

        private void AddError(ActionExecutingContext context, string errorMessage)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "file", new[] { errorMessage } }
            };

            var response = ApiResponseBuilder.BuildErrorResponse(
                data: errors,
                message: "File validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                reason: "Invalid file upload"
            );

            context.Result = new BadRequestObjectResult(response);
        }
    }
}