using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API
{
    public static class ResultHttpExtensions
    {
        public static ObjectResult ToProblemResult<T>(this Result<T> result)
        {
            var statusCode = result.ErrorType switch
            {
                ResultErrorType.Validation => StatusCodes.Status400BadRequest,
                ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ResultErrorType.NotFound => StatusCodes.Status404NotFound,
                ResultErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            var detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : result.Error;

            return new ObjectResult(new ProblemDetails { Status = statusCode, Detail = detail })
            {
                StatusCode = statusCode
            };
        }
    }
}
