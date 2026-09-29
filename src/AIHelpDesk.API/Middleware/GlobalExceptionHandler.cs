using Microsoft.AspNetCore.Diagnostics;

namespace AIHelpDesk.API.Middleware
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

            var statusCode = exception switch
            {
                ArgumentException =>  StatusCodes.Status400BadRequest,

                KeyNotFoundException => StatusCodes.Status404NotFound,

                InvalidOperationException => StatusCodes.Status409Conflict,

                _ => StatusCodes.Status500InternalServerError
            };

            var message = statusCode switch
            {
                StatusCodes.Status400BadRequest => "The request could not be completed.",

                StatusCodes.Status404NotFound => "The requested resource was not found.",

                StatusCodes.Status409Conflict => "The request conflicts with the current state of the resource.",

                _ => "An unexpected error occurred while processing the request."
            };

            var response = new
            {
                statusCode,
                message
            };

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(response,cancellationToken);

            return true;
        }
    }
}
