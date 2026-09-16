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
builder.Services.AddProblemDetails();
builder.Services
    .AddAccessModule()
    .AddRegistryModule()
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
