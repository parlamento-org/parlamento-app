using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

using Xunit;

namespace integration_tests;

public sealed class AuthSessionEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var user = new User
        {
            ProfilePic = 1,
            UserName = "session-tester",
            Email = "session-tester@example.com",
            Password = "hashed-password"
        };

        db.Users.Add(user);
        db.SaveChanges();

        UserId = user.Id;
    }
}

public sealed class AuthSessionEndpointsTests : IClassFixture<AuthSessionEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly AuthSessionEndpointsFactory _factory;

    public AuthSessionEndpointsTests(AuthSessionEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task SessionEndpointReturnsAuthenticatedUserSession()
    {
        var response = await _client.GetAsync("/auth/session");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("accessToken").GetString()));
        Assert.Equal(_factory.UserId, root.GetProperty("user").GetProperty("id").GetInt32());
        Assert.Equal("session-tester", root.GetProperty("user").GetProperty("userName").GetString());
    }
}
