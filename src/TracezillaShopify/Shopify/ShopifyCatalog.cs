using System.Text.Json;
using TracezillaShopify.Shared;

namespace TracezillaShopify.Shopify;

public sealed class ShopifyVariantMapper
{
    public CatalogItem? Map(JsonElement value)
    {
        var sku = value.TryGetProperty("sku", out var skuValue) ? skuValue.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(sku)) return null;
        var id = value.TryGetProperty("id", out var idValue) ? idValue.GetString() : null;
        if (string.IsNullOrEmpty(id)) throw new InvalidDataException("A Shopify variant is missing its ID.");
        var name = value.TryGetProperty("displayName", out var nameValue) ? nameValue.GetString()?.Trim() : null;
        return new(sku, id, string.IsNullOrEmpty(name) ? null : name);
    }
}

public sealed class ShopifyCatalogService(IGraphQlClient client, ShopifyVariantMapper mapper) : ICatalogReader
{
    public async Task<IReadOnlyList<CatalogItem>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CatalogItem>(); string? after = null;
        do {
            using var payload = await client.QueryAsync(GetProductVariants.Document, new { first = 250, after }, cancellationToken);
            if (!payload.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("productVariants", out var connection))
                throw new InvalidDataException("Shopify response is missing productVariants.");
            foreach (var value in connection.GetProperty("nodes").EnumerateArray()) { var item = mapper.Map(value); if (item is not null) items.Add(item); }
            var page = connection.GetProperty("pageInfo");
            if (!page.GetProperty("hasNextPage").GetBoolean()) break;
            after = page.GetProperty("endCursor").GetString();
            if (string.IsNullOrEmpty(after)) throw new InvalidDataException("Shopify pagination is missing an end cursor.");
        } while (true);
        return items;
    }
}
