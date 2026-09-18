using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fisiofit.Modules.Registry.Patients;

internal static class PatientsEndpoints
{
    public static IEndpointRouteBuilder MapPatientsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/patients", RegisterPatientAsync);
        endpoints.MapGet("/patients/{patientId:guid}", GetPatientAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterPatientAsync(
        RegisterPatientHttpRequest request,
        HttpContext httpContext,
        RegisterPatient handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        try
        {
            var result = await handler.ExecuteAsync(
                new RegisterPatientCommand(
                    request.FullName,
                    request.BirthDate,
                    request.Phone is null
                        ? null
                        : new PatientRegistrationPhone(
                            request.Phone.CountryCode ?? string.Empty,
                            request.Phone.AreaCode ?? string.Empty,
                            request.Phone.Number ?? string.Empty),
                    request.Cpf,
                    request.PrimaryUnitId,
                    request.RelationshipStartedOn,
                    request.PayerMode,
                    httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                    PatientRequestActor.FromPrincipal(httpContext.User),
                    traceId),
                cancellationToken);

            if (!result.Success)
            {
                return Problem(result, traceId);
            }

            var response = result.Value!;
            var location = $"/api/v1/patients/{response.PatientId:D}";
            httpContext.Response.Headers.Location = location;
            if (response.Replayed)
            {
                httpContext.Response.Headers["Idempotency-Replayed"] = "true";
            }

            return Results.Json(
                new RegisterPatientHttpResponse(response.PatientId, response.PersonId, response.Status),
                statusCode: StatusCodes.Status201Created);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency conflict",
                detail: "The resource changed while the request was being processed.",
                type: "https://api.fisiofit.example/problems/concurrency-conflict",
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["code"] = "CONCURRENCY_CONFLICT",
                    ["traceId"] = traceId
                });
        }
        catch (Exception)
        {
            loggerFactory.CreateLogger("Fisiofit.Registry.Patients")
                .LogError("RegisterPatient failed. TraceId: {TraceId}", traceId);
            return InternalProblem(traceId);
        }
    }

    private static async Task<IResult> GetPatientAsync(
        Guid patientId,
        HttpContext httpContext,
        GetPatientDetails handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        try
        {
            var result = await handler.ExecuteAsync(
                patientId,
                PatientRequestActor.FromPrincipal(httpContext.User),
                cancellationToken);
            if (!result.Success)
            {
                return Problem(result, traceId);
            }

            httpContext.Response.Headers.CacheControl = "no-store";
            return Results.Ok(result.Value);
        }
        catch (Exception)
        {
            loggerFactory.CreateLogger("Fisiofit.Registry.Patients")
                .LogError("GetPatient failed. TraceId: {TraceId}", traceId);
            return InternalProblem(traceId);
        }
    }

    private static IResult Problem<T>(PatientApplicationResult<T> result, string traceId)
    {
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = result.Code,
            ["traceId"] = traceId
        };
        if (result.Errors is not null)
        {
            extensions["errors"] = result.Errors;
        }

        return Results.Problem(
            statusCode: result.StatusCode,
            title: Title(result.StatusCode),
            detail: result.Detail,
            type: $"https://api.fisiofit.example/problems/{result.Code!.ToLowerInvariant().Replace('_', '-')}",
            extensions: extensions);
    }

    private static IResult InternalProblem(string traceId) => Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Internal server error",
        detail: "An unexpected error occurred.",
        type: "https://api.fisiofit.example/problems/internal-error",
        extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = "INTERNAL_ERROR",
            ["traceId"] = traceId
        });

    private static string Title(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Invalid request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Business rule violation",
        StatusCodes.Status503ServiceUnavailable => "Dependency unavailable",
        _ => "Request failed"
    };

    private sealed record RegisterPatientHttpRequest(
        string? FullName,
        DateOnly BirthDate,
        PhoneHttpRequest? Phone,
        string? Cpf,
        Guid PrimaryUnitId,
        DateOnly RelationshipStartedOn,
        string? PayerMode);

    private sealed record PhoneHttpRequest(string? CountryCode, string? AreaCode, string? Number);

    private sealed record RegisterPatientHttpResponse(Guid PatientId, Guid PersonId, string Status);
}
