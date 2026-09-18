using Fisiofit.Modules.Access;
using Fisiofit.Modules.Audit;
using Fisiofit.Modules.Clinical;
using Fisiofit.Modules.Communication;
using Fisiofit.Modules.Crm;
using Fisiofit.Modules.Documents;
using Fisiofit.Modules.Operations;
using Fisiofit.Modules.Registry;
using Fisiofit.Modules.Reports;
using Fisiofit.Modules.Revenue;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "code",
            context.ProblemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => "VALIDATION_ERROR",
                StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
                StatusCodes.Status403Forbidden => "FORBIDDEN",
                StatusCodes.Status404NotFound => "RESOURCE_NOT_FOUND",
                StatusCodes.Status409Conflict => "CONFLICT",
                _ => "INTERNAL_ERROR"
            });
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
        if (context.ProblemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Detail = "An unexpected error occurred.";
        }
    };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services
    .AddAccessModule()
    .AddRegistryModule(builder.Configuration)
    .AddCrmModule()
    .AddOperationsModule()
    .AddClinicalModule()
    .AddRevenueModule()
    .AddCommunicationModule()
    .AddDocumentsModule()
    .AddAuditModule()
    .AddReportsModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapAccessEndpoints();
api.MapRegistryEndpoints();
api.MapCrmEndpoints();
api.MapOperationsEndpoints();
api.MapClinicalEndpoints();
api.MapRevenueEndpoints();
api.MapCommunicationEndpoints();
api.MapDocumentsEndpoints();
api.MapAuditEndpoints();
api.MapReportsEndpoints();

app.Run();

public partial class Program;
