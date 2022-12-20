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
        Brand brand3 = new Brand("Maersk");
        brand3.SalesChannels[0].isActive = true;
        brand3.SalesChannels[0].fullOrWait = true;


        db.brands!.AddRange(new List<Brand>
            {
                new Brand("Levis"),
                new Brand("Nike"),
                brand3,
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
    public async Task GetBrands()
    {
        var response = await _client.GetAsync("/brand");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var brandList = JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<Brand>>>(responseString);
        Assert.Equal(3, brandList["brand"].Count());
    }

    [Fact, Order(1)]
    public async Task GetBrand()
    {
        var response = await _client.GetAsync("/brand/1");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var brand = JsonConvert.DeserializeObject<Brand>(responseString);
        Assert.Equal("Levis", brand.Name);
        Assert.Equal(1, brand.Id);
        Assert.Empty(brand.contactInfos);
        Assert.NotEmpty(brand.SalesChannels);

    }

    [Fact, Order(2)]
    public async Task GetBrandNotFound()
    {
        var response = await _client.GetAsync("/brand/4");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact, Order(3)]
    public async Task CreateBrand()
    {
        var brand = new Brand();
        brand.Name = "Nike 2";

        var response = await _client.PostAsync("/brand", new StringContent(JsonConvert.SerializeObject(brand), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var createdBrand = JsonConvert.DeserializeObject<Brand>(responseString);
        Assert.Equal("Nike 2", createdBrand.Name);
        Assert.Empty(createdBrand.contactInfos);
        Assert.Empty(createdBrand.SalesChannels);

    }
    [Fact, Order(4)]
    public async Task DeleteBrand()
    {

        var response = await _client.DeleteAsync("/brand/2");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var deletedBrand = JsonConvert.DeserializeObject<Brand>(responseString);
        Assert.Equal("Nike", deletedBrand.Name);

        Assert.Empty(deletedBrand.contactInfos);
        Assert.NotEmpty(deletedBrand.SalesChannels);

    }

    [Fact, Order(5)]
    public async Task EditBrandFullfilment()
    {
        var fullfilmentDTO = new FullfilmentDTO();
        fullfilmentDTO.salesChannelName = SalesChannelName.Webshop;
        fullfilmentDTO.brandID = 1;
        fullfilmentDTO.isActive = true;
        fullfilmentDTO.fullOrWait = true;
        fullfilmentDTO.partialAndCancel = false;

        var response = await _client.PutAsync("/edit/sales-channel", new StringContent(JsonConvert.SerializeObject(fullfilmentDTO), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var salesChannel = JsonConvert.DeserializeObject<SalesChannel>(responseString);
        Assert.Equal(SalesChannelName.Webshop, salesChannel.Name);
        Assert.True(salesChannel.isActive);
        Assert.False(salesChannel.partialAndCancel);
        Assert.True(salesChannel.fullOrWait);

        fullfilmentDTO.salesChannelName = SalesChannelName.Webshop;
        fullfilmentDTO.brandID = 1;
        fullfilmentDTO.isActive = false;
        fullfilmentDTO.fullOrWait = true;
        fullfilmentDTO.partialAndCancel = false;

        response = await _client.PutAsync("/edit/sales-channel", new StringContent(JsonConvert.SerializeObject(fullfilmentDTO), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        responseString = await response.Content.ReadAsStringAsync();

        salesChannel = JsonConvert.DeserializeObject<SalesChannel>(responseString);
        Assert.Equal(SalesChannelName.Webshop, salesChannel.Name);
        Assert.False(salesChannel.isActive);
        Assert.False(salesChannel.partialAndCancel);
        Assert.False(salesChannel.fullOrWait);

    }

    [Fact, Order(6)]
    public async Task EditReturnPolicy()
    {
        var returnDTO = new ReturnDTO();
        returnDTO.salesChannelName = SalesChannelName.StockShared;
        returnDTO.brandID = 1;
        returnDTO.prePrintedReturnLabel = true;
        returnDTO.digitalReturnLabel = false;

        var response = await _client.PutAsync("/edit/return-policy", new StringContent(JsonConvert.SerializeObject(returnDTO), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);

    }

    [Fact, Order(6)]
    public async Task EditContactInfo()
    {
        var contactInfoDTO = new ContactInfoDTO();
        contactInfoDTO.newBrandName = "New Brand Name";
        contactInfoDTO.brandID = 1;
        contactInfoDTO.contactInfos = new List<ContactInfo>();

        var response = await _client.PutAsync("/edit/contact-info", new StringContent(JsonConvert.SerializeObject(contactInfoDTO), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    }

    [Fact, Order(6)]
    public async Task GetBrandChannels()
    {
        var response = await _client.GetAsync("/brand/3/channels");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var salesChannels = JsonConvert.DeserializeObject<List<SalesChannel>>(responseString);
        Assert.True(salesChannels[0].isActive);
        Assert.True(salesChannels[0].fullOrWait);
        Assert.False(salesChannels[0].partialAndCancel);

        Assert.False(salesChannels[1].isActive);
        Assert.False(salesChannels[1].fullOrWait);
        Assert.False(salesChannels[1].partialAndCancel);
    }
}