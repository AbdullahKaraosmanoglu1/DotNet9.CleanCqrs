using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace DotNet9.Api.Configuration;

public static class VersioningExtensions
{
    public static IServiceCollection AddApiVersioningAndExplorer(this IServiceCollection services)
    {
        services
            .AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version"),
                    new QueryStringApiVersionReader("api-version"));
            })
            .AddApiExplorer(opt =>
            {
                opt.GroupNameFormat = "'v'VVV";     // => v1, v2
                opt.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
