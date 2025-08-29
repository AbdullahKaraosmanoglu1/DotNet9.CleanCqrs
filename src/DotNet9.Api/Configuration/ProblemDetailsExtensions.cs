using System.Net;
using DotNet9.Application.Users.Exceptions;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

namespace DotNet9.Api.Configuration;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddProblemDetailsConfigured(this IServiceCollection services)
    {
        services.AddProblemDetails(opt =>
        {
            // Prod'da detayları gizle, Dev'de göster
            opt.IncludeExceptionDetails = (ctx, ex) =>
            {
                var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
                return env.IsDevelopment();
            };

            // ---- Exception → StatusCode eşlemeleri ----
            opt.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);
            opt.MapToStatusCode<ForbiddenException>(StatusCodes.Status403Forbidden); // kendi özel istisnan varsa
            opt.MapToStatusCode<KeyNotFoundException>(StatusCodes.Status404NotFound);
            opt.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
            opt.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
            // İptal edilen istekler için 499 (Nginx ekosisteminde yaygın), ASP.NET’te 400 döndürmek daha yaygın
            opt.MapToStatusCode<OperationCanceledException>(StatusCodes.Status400BadRequest);

            // FluentValidation → ValidationProblemDetails (400)
            opt.Map<ValidationException>((ctx, ex) =>
            {
                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

                var vpd = new ValidationProblemDetails(errors)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://httpstatuses.io/400"
                };

                // trace/correlation enrich
                vpd.Extensions["traceId"] = ctx.TraceIdentifier;
                var corr = ctx.Response.Headers[Middleware.CorrelationIdMiddleware.HeaderName].ToString();
                if (!string.IsNullOrWhiteSpace(corr))
                    vpd.Extensions["correlationId"] = corr;

                return vpd;
            });
            opt.Map<DuplicateEmailException>(ex => new ProblemDetails
            {
                Title = "Duplicate email",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.io/409"
            });

            // Yazmadan önce tüm ProblemDetails'ları zenginleştir (traceId, correlationId vs.)
            opt.OnBeforeWriteDetails = (ctx, pd) =>
            {
                pd.Extensions["traceId"] = ctx.TraceIdentifier;

                var corr = ctx.Response.Headers[Middleware.CorrelationIdMiddleware.HeaderName].ToString();
                if (!string.IsNullOrWhiteSpace(corr))
                    pd.Extensions["correlationId"] = corr;
            };

            // Log seviyesi kontrolü (opsiyonel)
            opt.ShouldLogUnhandledException = (ctx, ex, pd) => true; // hepsini logla (Serilog devrede)
        });

        return services;
    }

    // Örnek özel istisna (yoksa silebilirsin)
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException(string? message = null) : base(message ?? "Forbidden") { }
    }
}
