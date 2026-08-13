using System.Text.Json;
using TracezillaShopify.Shopify;

public sealed class ShopifyLocationsTests
{
    private const string Location = """{"id":"gid://shopify/Location/1","legacyResourceId":"1","name":"Development Warehouse","isActive":true,"hasActiveInventory":true,"fulfillsOnlineOrders":true,"address":{"address1":"Banana Street 1","address2":null,"city":"Copenhagen","province":null,"country":"Denmark","zip":"1000"}}""";
    [Fact] public void MapsLocation() { using var json=JsonDocument.Parse(Location);var result=ShopifyLocationService.Map(json.RootElement);Assert.Equal("Development Warehouse",result.Name);Assert.True(result.IsActive); }
    [Fact] public async Task PaginatesLocations() { var client=new FakeClient();var result=await new ShopifyLocationService(client).ReadAsync();Assert.Equal(2,result.Count); }
    private sealed class FakeClient : IGraphQlClient { private int page; public Task<JsonDocument> QueryAsync(string query,object variables,CancellationToken cancellationToken=default){page++;var pageInfo=page==1?"{\"hasNextPage\":true,\"endCursor\":\"next\"}":"{\"hasNextPage\":false,\"endCursor\":null}";return Task.FromResult(JsonDocument.Parse("{\"data\":{\"locations\":{\"nodes\":["+Location+"],\"pageInfo\":"+pageInfo+"}}}"));} }
}
