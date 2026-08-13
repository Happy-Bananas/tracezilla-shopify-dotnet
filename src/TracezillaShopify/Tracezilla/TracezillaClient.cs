using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TracezillaShopify.Tracezilla;

public interface IJsonClient
{
    Task<JsonDocument> GetAsync(string path, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default);
    Task<JsonDocument> PostAsync(string path, object payload, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

public sealed class TracezillaClient(Configuration configuration) : IJsonClient
{
    private readonly HttpClient _http = CreateClient(configuration);

    public async Task<JsonDocument> GetAsync(string path, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        var url = $"{configuration.TracezillaBaseUrl}/api/v1/{configuration.TracezillaTeamSlug}/{path.TrimStart('/')}?{string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
        using var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"tracezilla request failed with HTTP {(int)response.StatusCode}.");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }
    public async Task<JsonDocument> PostAsync(string path, object payload, CancellationToken cancellationToken = default)
    {
        var url = $"{configuration.TracezillaBaseUrl}/api/v1/{configuration.TracezillaTeamSlug}/{path.TrimStart('/')}";
        using var response = await _http.PostAsJsonAsync(url, payload, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"tracezilla request failed with HTTP {(int)response.StatusCode}.");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private static HttpClient CreateClient(Configuration configuration)
    {
        var client = new HttpClient { Timeout = configuration.Timeout };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.TracezillaApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
