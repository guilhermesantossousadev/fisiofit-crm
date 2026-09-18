using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isInvalidRequest = exception is BadHttpRequestException or JsonException;
        var status = isInvalidRequest
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        if (!isInvalidRequest)
        {
            logger.LogError("Unhandled API failure. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = isInvalidRequest ? "Invalid request" : "Internal server error",
                Detail = isInvalidRequest ? "The request body is invalid." : "An unexpected error occurred."
            },
            Exception = exception
        });
    }
}
