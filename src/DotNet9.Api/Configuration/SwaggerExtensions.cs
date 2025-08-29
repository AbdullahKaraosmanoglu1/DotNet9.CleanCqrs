using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace DotNet9.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithVersioning(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.ConfigureOptions<ConfigureSwaggerPerApiVersion>();
        return services;
    }
}

public sealed class ConfigureSwaggerPerApiVersion
    : IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    public ConfigureSwaggerPerApiVersion(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        // Provider’dan gelen versiyonları kaydet
        var descs = _provider.ApiVersionDescriptions.ToList();
        foreach (var d in descs)
        {
            if (!options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(d.GroupName))
            {
                options.SwaggerDoc(d.GroupName, new OpenApiInfo
                {
                    Title = "DotNet9 API",
                    Version = d.ApiVersion.ToString()
                });
            }
        }

        // Güvenlik ağı: hiç sürüm bulunamadıysa en azından v1 üret
        if (descs.Count == 0 && !options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"))
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "DotNet9 API", Version = "1.0" });
        }
    }
}
