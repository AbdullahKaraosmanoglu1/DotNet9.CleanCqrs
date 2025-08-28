using Microsoft.AspNetCore.HttpOverrides; // Proxy arkasýnda gerçek IP için (opsiyonel)
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using MediatR;
using FluentValidation;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using DotNet9.Api.Middleware;
using DotNet9.Api.Common;
using DotNet9.Application.Common.Behaviors;
using DotNet9.Infrastructure.Persistence;
using DotNet9.Infrastructure.Users;                   // Implementations
using DotNet9.Application.Users.Abstractions;         // Interfaces
using DotNet9.Application.Users.Commands.RegisterUser;
using DotNet9.Application.Users.Queries.GetUser;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// EF Core (Postgres)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// MediatR + FluentValidation (Application assembly tara)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterUserHandler).Assembly);

// Infrastructure DI (repo + read service)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserReadService, UserReadService>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// OpenTelemetry (traces + metrics)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "DotNet9.Api",
        serviceVersion: "1.0.0"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation();
        // t.AddNpgsql(); // Npgsql paketini eklediysen aç
        t.AddConsoleExporter(); // Þimdilik Console'a trace yaz
        // OTLP kullanacaksan:
        // t.AddOtlpExporter(); // Default localhost:4317/4318 (collector gerekir)
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddRuntimeInstrumentation();
        m.AddProcessInstrumentation();
        m.AddMeter("Microsoft.AspNetCore.Hosting");
        m.AddPrometheusExporter(); // /metrics endpoint'i için
    });

// Swagger + HealthChecks
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DotNet9 API", Version = "v1" });
    // Ýstersen XML comments için:
    // var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml), true);
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    // Log satýrýna custom alanlar ekle
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("CorrelationId", http.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        diag.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
        diag.Set("RequestPath", http.Request.Path);
        diag.Set("QueryString", http.Request.QueryString.ToString());
        diag.Set("Method", http.Request.Method);
        // yanýt kodu SerilogRequestLogging tarafýndan otomatik eklenir
    };
});
app.UseMiddleware<CorrelationIdMiddleware>();   // <-- correlation id
app.UseMiddleware<ExceptionHandlingMiddleware>(); // <-- global hata yakalama
// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Swagger (geliþtirme ortamýnda)
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Test"))
{
    app.UseHttpLogging();             // varsa
    app.UseSerilogRequestLogging();   // varsa
    app.UseResponseCompression();     // varsa
}


app.MapHealthChecks("/health");

// ---- Minimal API endpoints ----

// Register user
app.MapPost("/api/users", async (RegisterUserCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(cmd, ct);
    return Results.Created($"/api/users/{id}", ApiResponse<object>.Ok(new { id }));
})
.WithName("RegisterUser")
.WithOpenApi();

// Get user
app.MapGet("/api/users/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var dto = await mediator.Send(new GetUserQuery(id), ct);
    return Results.Ok(ApiResponse<object>.Ok(dto));
})
.WithName("GetUser")
.WithOpenApi();

// Alive
app.MapGet("/", () => Results.Ok(ApiResponse<object>.Ok(new { ok = true, ts = DateTimeOffset.UtcNow })));

app.Run();
