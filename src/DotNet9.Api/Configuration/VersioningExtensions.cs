using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace DotNet9.Api.Configuration;

public static class VersioningExtensions
{
    public static IServiceCollection AddApiVersioningAndExplorer(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
        {
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.ReportApiVersions = true;
        })
        .AddApiExplorer(opt =>
        {
            // 'v1' istiyorsan "'v'V" kullan; 'v1.0' istiyorsan "'v'VVV"
            opt.GroupNameFormat = "'v'V";                // -> v1
            opt.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
