using System.Net.Http.Json;
using System.Text.Json;

namespace TracezillaShopify.Shopify;

public interface IGraphQlClient
{
    Task<JsonDocument> QueryAsync(string query, object variables, CancellationToken cancellationToken = default);
}

public sealed class ShopifyClient(Configuration configuration) : IGraphQlClient
{
    private readonly HttpClient _http = new() { Timeout = configuration.Timeout };
    private string? _token;

    public async Task<JsonDocument> QueryAsync(string query, object variables, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://{configuration.ShopifyShopUrl}/admin/api/{configuration.ShopifyApiVersion}/graphql.json");
        request.Headers.Add("X-Shopify-Access-Token", await AccessTokenAsync(cancellationToken));
        request.Content = JsonContent.Create(new { query, variables });
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Shopify request failed with HTTP {(int)response.StatusCode}.");
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (document.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            throw new InvalidDataException("Shopify rejected the GraphQL query.");
        return document;
    }

    private async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null) return _token;
        using var response = await _http.PostAsync($"https://{configuration.ShopifyShopUrl}/admin/oauth/access_token",
            new FormUrlEncodedContent(new Dictionary<string, string> {
                ["grant_type"] = "client_credentials", ["client_id"] = configuration.ShopifyClientId,
                ["client_secret"] = configuration.ShopifyClientSecret, ["scope"] = configuration.ShopifyScope }), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Shopify authentication failed with HTTP {(int)response.StatusCode}.");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        _token = document.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
        return _token ?? throw new InvalidDataException("Shopify authentication did not return an access token.");
    }
}
