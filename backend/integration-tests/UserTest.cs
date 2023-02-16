using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using backend.Models;

using Newtonsoft.Json;

using Xunit;
using Xunit.Extensions.Ordering;

namespace integration_tests;

public class AsnFactory : TestingWebAppFactory
{
    protected override void SeedDbForTests(DatabaseContext db)
    {
        User user1 = new User();
        user1.ProfilePic = 1;
        user1.UserName = "radiodomiguel70";
        user1.Email = "radiodomiguel70@gmail.com";
        user1.Password = "123456";

        User user2 = new User();
        user2.ProfilePic = 2;
        user2.UserName = "joan";
        user2.Email = "joan@gmail.com";
        user2.Password = "12345";

        db.Users!.AddRange(new List<User>
            {
                user1,
                user2
            });
        db.SaveChanges();
    }
}

[Order(1)]
public class AsnIT : IClassFixture<AsnFactory>
{
    private readonly HttpClient _client;

    public AsnIT(AsnFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact, Order(0)]
    public async Task GetUsers()
    {
        var response = await _client.GetAsync("/user");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var userList = JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<User>>>(responseString);
        Assert.Equal(2, userList["users"].Count());
    }

}
