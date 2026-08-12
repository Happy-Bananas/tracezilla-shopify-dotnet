using TracezillaShopify.Shared;

namespace TracezillaShopify.Workflows;

public sealed record CatalogComparisonResult(int DisplayLimit, IReadOnlyList<string> PresentInBoth,
    IReadOnlyList<string> OnlyInShopify, IReadOnlyList<string> OnlyInTracezilla)
{
    public string Status => OnlyInShopify.Count == 0 && OnlyInTracezilla.Count == 0 ? "match" : "differences";
}

public sealed class CompareCatalogs(ICatalogReader shopify, ICatalogReader tracezilla)
{
    public async Task<CatalogComparisonResult> RunAsync(int displayLimit = 10)
    {
        if (displayLimit < 1) throw new ArgumentOutOfRangeException(nameof(displayLimit), "The display limit must be positive.");
        var reads = await Task.WhenAll(shopify.ReadAsync(), tracezilla.ReadAsync());
        var shopifySkus = reads[0].Select(item => item.Sku).ToHashSet(StringComparer.Ordinal);
        var tracezillaSkus = reads[1].Select(item => item.Sku).ToHashSet(StringComparer.Ordinal);
        return new(displayLimit, shopifySkus.Intersect(tracezillaSkus).Order().ToArray(),
            shopifySkus.Except(tracezillaSkus).Order().ToArray(), tracezillaSkus.Except(shopifySkus).Order().ToArray());
    }
}
