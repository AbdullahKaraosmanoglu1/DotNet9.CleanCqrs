using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DotNet9.IntegrationTests._Infra;
using Xunit;

namespace DotNet9.IntegrationTests.Users;

public class UsersEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private sealed record CreateReq(string Email, string Username);
    private sealed record IdRes(Guid Id);
    private sealed record ApiResponse<T>(bool Success, T? Data, string[]? Errors);

    [Fact]
    public async Task Register_Then_Get_Should_Return_User()
    {
        // 1) POST /api/users
        var create = new CreateReq("ituser@demo.com", "ituser");
        var post = await _client.PostAsJsonAsync("/api/users", create);

        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await post.Content.ReadFromJsonAsync<ApiResponse<IdRes>>();
        created.Should().NotBeNull();
        created!.Success.Should().BeTrue();
        created.Data!.Id.Should().NotBeEmpty();

        var id = created.Data.Id;

        // 2) GET /api/users/{id}
        var get = await _client.GetAsync($"/api/users/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        // dinamik okuma
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("id").GetGuid().Should().Be(id);
        data.GetProperty("email").GetString().Should().Be("ituser@demo.com");
        data.GetProperty("username").GetString().Should().Be("ituser");
    }

    [Fact]
    public async Task Get_Unknown_Id_Should_Return_404()
    {
        var res = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Success.Should().BeFalse();
        body.Errors.Should().NotBeNull();
        body.Errors!.Length.Should().BeGreaterThan(0);
    }
}
