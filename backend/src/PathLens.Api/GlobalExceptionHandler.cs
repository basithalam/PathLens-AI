using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PathLens.Api;

/// <summary>
/// Maps known client errors to honest status codes. Malformed JSON is a
/// 400, not a 500; everything unknown stays a generic 500 without details.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning("Bad request: {Message}", badRequest.Message);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request body",
                Detail = "The request body could not be read. Check that JSON and field formats are valid.",
            }, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled error while processing a request.");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred while processing your request.",
        }, cancellationToken);
        return true;
    }
}
