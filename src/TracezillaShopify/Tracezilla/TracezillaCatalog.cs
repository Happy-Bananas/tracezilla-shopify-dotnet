using System.Text.Json;
using TracezillaShopify.Shared;

namespace TracezillaShopify.Tracezilla;

public sealed class TracezillaSkuMapper
{
    public CatalogItem? Map(JsonElement value)
    {
        var sku = value.TryGetProperty("sku_code", out var skuValue) ? skuValue.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(sku)) return null;
        var id = value.TryGetProperty("id", out var idValue) ? idValue.ToString() : sku;
        return new(sku, id);
    }
}

public sealed class TracezillaCatalogService(IJsonClient client, TracezillaSkuMapper mapper) : ICatalogReader
{
    public async Task<IReadOnlyList<CatalogItem>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CatalogItem>();
        var query = new Dictionary<string, string> { ["sortBy"] = "sku_code", ["sortDirection"] = "asc", ["perPage"] = "250" };
        var visited = new HashSet<string>();
        do {
            using var payload = await client.GetAsync("skus", query, cancellationToken);
            if (!payload.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("tracezilla response is missing SKU data.");
            foreach (var value in data.EnumerateArray()) { var item = mapper.Map(value); if (item is not null) items.Add(item); }
            var next = payload.RootElement.TryGetProperty("links", out var links) && links.TryGetProperty("next_page", out var nextValue) && nextValue.ValueKind == JsonValueKind.String ? nextValue.GetString() : null;
            if (string.IsNullOrEmpty(next)) break;
            var parameters = new Uri(next).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            if (parameters.Length == 0) throw new InvalidDataException("tracezilla returned no next-page parameters.");
            foreach (var parameter in parameters) { var parts = parameter.Split('=', 2); query[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts.ElementAtOrDefault(1) ?? ""); }
            var fingerprint = string.Join('&', query.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
            if (!visited.Add(fingerprint)) throw new InvalidDataException("tracezilla returned the same next page repeatedly.");
        } while (true);
        return items;
    }
    public async Task<IReadOnlyList<string>> ExistingSkuCodesAsync(CancellationToken cancellationToken = default) => (await ReadAsync(cancellationToken)).Select(x => x.Sku).Distinct().ToArray();
    public async Task CreateSkuAsync(object payload, CancellationToken cancellationToken = default) { using var _ = await client.PostAsync("skus", payload, cancellationToken); }
}
