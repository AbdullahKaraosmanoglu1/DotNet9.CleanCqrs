using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using DotNet9.Infrastructure.Persistence;
using Xunit;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace DotNet9.IntegrationTests._Infra;

public sealed class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer _pg;

    public TestWebAppFactory()
    {
        // Docker’a local npipe ile bağlan; proxy’leri kapat
        Environment.SetEnvironmentVariable("DOCKER_HOST", "npipe://./pipe/docker_engine");
        Environment.SetEnvironmentVariable("HTTP_PROXY", null);
        Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        Environment.SetEnvironmentVariable("ALL_PROXY", null);

        _pg = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "clean_cqrs_it",
                Username = "postgres",
                Password = "Postgres123!"
            })
            .WithImage("postgres:16")
            .WithCleanUp(false)              // <<< Ryuk kapalı
            .Build();
    }

    public string ConnectionString => _pg.ConnectionString;

    public async Task InitializeAsync() => await _pg.StartAsync();

    // Reaper kapalı olduğundan temizlik bize ait
    public async Task DisposeAsync()
    {
        try { await _pg.StopAsync(); } catch { /* yoksay */ }
        await _pg.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionString));
        });
    }
}
