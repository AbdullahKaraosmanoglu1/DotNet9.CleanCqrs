using DotNet9.Api.Middleware;
using DotNet9.Application.Users.Commands.RegisterUser;
using DotNet9.Application.Users.Abstractions;
using DotNet9.Infrastructure.Users;
using DotNet9.Infrastructure.Persistence;
using DotNet9.Api.Configuration; // extension sýnýflarý

using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Hellang.Middleware.ProblemDetails;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Asp.Versioning.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

// --------------------- Logging ---------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// --------------------- DbContext ---------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// --------------------- MediatR + Validation ---------------------
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterUserHandler).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// --------------------- Controllers ---------------------
builder.Services.AddControllers();

// --------------------- API Versioning + Swagger ---------------------
builder.Services.AddApiVersioningAndExplorer();   // Asp.Versioning + Explorer
builder.Services.AddSwaggerWithVersioning();      // Swagger dokümanlarý

// --------------------- ProblemDetails ---------------------
builder.Services.AddProblemDetailsConfigured();   // RFC7807 hata yanýtlarý

// --------------------- Infrastructure DI ---------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserReadService, UserReadService>();
// builder.Services.AddInfrastructure(); // Eðer senin extension’ýnda tekrar repo kayýtlarý varsa ÇIKAR

// --------------------- Health/Compression/RateLimit ---------------------
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();
builder.Services.AddRateLimiter(_ => { /* rate limit policy */ });

// --------------------- OpenTelemetry ---------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("DotNet9.Api", serviceVersion: "1.0.0"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation();
        t.AddConsoleExporter();
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddRuntimeInstrumentation();
        m.AddProcessInstrumentation();
        m.AddPrometheusExporter();
    });

// ==============================================================
var app = builder.Build();

// --------------------- Middleware Pipeline ---------------------
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseProblemDetails();

app.UseResponseCompression();
app.UseRateLimiter();

// --------------------- Swagger ---------------------
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test"))
{
    var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();

    app.UseSwagger(); // <-- önce bu
    app.UseSwaggerUI(opt =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            opt.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json",
                $"DotNet9.Api {desc.GroupName.ToUpperInvariant()}");
        }
        opt.RoutePrefix = "swagger"; // /swagger/index.html
    });
}


// --------------------- Endpoints ---------------------
app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
