using System.Text.Json;

namespace TracezillaShopify.Shopify;

public sealed record ShopifyAddress(string? Address1, string? Address2, string? City, string? Province, string? Country, string? Zip);
public sealed record ShopifyLocation(string GraphQlId, string LegacyId, string Name, bool IsActive, bool HasActiveInventory, bool FulfillsOnlineOrders, ShopifyAddress Address);

public sealed class ShopifyLocationService(IGraphQlClient client)
{
    public const string Query = """
        query GetLocations($first: Int!, $after: String) {
          locations(first: $first, after: $after) { nodes { id legacyResourceId name isActive hasActiveInventory fulfillsOnlineOrders address { address1 address2 city province country zip } } pageInfo { hasNextPage endCursor } }
        }
        """;

    public async Task<IReadOnlyList<ShopifyLocation>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ShopifyLocation>(); string? after = null; var seen = new HashSet<string>();
        while (true) {
            using var payload = await client.QueryAsync(Query, new { first = 250, after }, cancellationToken);
            if (!payload.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("locations", out var connection) || !connection.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array || !connection.TryGetProperty("pageInfo", out var page)) throw new InvalidDataException("Shopify response is missing locations.");
            foreach (var node in nodes.EnumerateArray()) result.Add(Map(node));
            if (!page.TryGetProperty("hasNextPage", out var hasNextNode) || hasNextNode.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new InvalidDataException("Shopify returned invalid location pagination data.");
            if (!hasNextNode.GetBoolean()) break;
            after = page.TryGetProperty("endCursor", out var cursor) ? cursor.GetString() : null;
            if (string.IsNullOrEmpty(after) || !seen.Add(after)) throw new InvalidDataException("Shopify returned an invalid or repeated location cursor.");
        }
        return result;
    }

    public static ShopifyLocation Map(JsonElement value)
    {
        string Required(string name) { if (!value.TryGetProperty(name, out var field) || string.IsNullOrWhiteSpace(field.ToString())) throw new InvalidDataException($"Shopify location field [{name}] is required."); return field.ToString().Trim(); }
        bool Boolean(string name) { if (!value.TryGetProperty(name, out var field) || field.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new InvalidDataException($"Shopify location field [{name}] must be boolean."); return field.GetBoolean(); }
        var address = value.TryGetProperty("address", out var rawAddress) && rawAddress.ValueKind == JsonValueKind.Object ? rawAddress : default;
        string? Optional(string name) => address.ValueKind == JsonValueKind.Object && address.TryGetProperty(name, out var field) && field.ValueKind != JsonValueKind.Null && !string.IsNullOrWhiteSpace(field.ToString()) ? field.ToString().Trim() : null;
        return new(Required("id"), Required("legacyResourceId"), Required("name"), Boolean("isActive"), Boolean("hasActiveInventory"), Boolean("fulfillsOnlineOrders"), new(Optional("address1"), Optional("address2"), Optional("city"), Optional("province"), Optional("country"), Optional("zip")));
    }
}
