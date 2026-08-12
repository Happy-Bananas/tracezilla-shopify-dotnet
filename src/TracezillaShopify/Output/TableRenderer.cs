using TracezillaShopify.Workflows;

namespace TracezillaShopify.Output;

public static class TableRenderer
{
    public static string Render(CatalogComparisonResult result)
    {
        var rows = result.PresentInBoth.Take(result.DisplayLimit).Select(sku => (sku, "Yes", "Yes", "Match"))
            .Concat(result.OnlyInShopify.Take(result.DisplayLimit).Select(sku => (sku, "Yes", "No", "Missing in tracezilla")))
            .Concat(result.OnlyInTracezilla.Take(result.DisplayLimit).Select(sku => (sku, "No", "Yes", "Missing in Shopify"))).OrderBy(row => row.sku);
        var lines = new List<string> { $"{"SKU",-24} {"Shopify",-10} {"tracezilla",-12} Result", new('-', 72) };
        lines.AddRange(rows.Select(row => $"{row.sku,-24} {row.Item2,-10} {row.Item3,-12} {row.Item4}"));
        lines.AddRange(["", $"Matched: {result.PresentInBoth.Count}; missing in tracezilla: {result.OnlyInShopify.Count}; missing in Shopify: {result.OnlyInTracezilla.Count}", $"Showing at most {result.DisplayLimit} rows from each result category."]);
        return string.Join(Environment.NewLine, lines);
    }
}
