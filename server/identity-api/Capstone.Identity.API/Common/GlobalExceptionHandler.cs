using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Common
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = httpContext.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Something went wrong on our end. Please try again later."
            };
            problem.Extensions["traceId"] = traceId;

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(
                problem, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }
    }
}
