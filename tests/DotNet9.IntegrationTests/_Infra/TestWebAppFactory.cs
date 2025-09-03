using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet9.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

// !!! ÖNEMLİ: Aşağıdaki satırı KALDIR.
// using Microsoft.VisualStudio.TestPlatform.TestHost;

// API projesinin Program'ını görünür kılmak için API'de Program.cs sonuna şunu eklemiş olmalısın:
// public partial class Program { }

using Xunit; // IAsyncLifetime buradan gelir

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
         .WithCleanUp(false)
         .Build();
         }

    public string ConnectionString => _pg.ConnectionString;

    // xUnit: test yaşam döngüsü başlangıcı
    public async Task InitializeAsync() => await _pg.StartAsync();

    // xUnit: test yaşam döngüsü bitişi - explicit interface implementation
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync().AsTask(); // override edilen versiyona yönlendir
    }

    // WebApplicationFactory yaşam döngüsü (override) — uyarıyı kaldırır
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await _pg.StopAsync();
        }
        catch
        {
            // yoksay
        }

        await _pg.DisposeAsync();

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Var olan AppDbContext kaydını sök
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            // Test DB'ye yönlendir
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionString));
        });
    }
}
