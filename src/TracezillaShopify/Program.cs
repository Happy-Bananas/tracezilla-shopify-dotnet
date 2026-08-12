using System.Text.Json;
using TracezillaShopify;
using TracezillaShopify.Output;
using TracezillaShopify.Shopify;
using TracezillaShopify.Tracezilla;
using TracezillaShopify.Workflows;

try {
    var json = args.Contains("--json");
    var limitArgument = args.FirstOrDefault(value => value.StartsWith("--limit=", StringComparison.Ordinal));
    var limit = limitArgument is null ? 10 : int.Parse(limitArgument[8..]);
    var configuration = Configuration.FromEnvironment();
    var result = await new CompareCatalogs(
        new ShopifyCatalogService(new ShopifyClient(configuration), new ShopifyVariantMapper()),
        new TracezillaCatalogService(new TracezillaClient(configuration), new TracezillaSkuMapper())).RunAsync(limit);
    Console.WriteLine(json ? JsonSerializer.Serialize(new {
        status = result.Status, display_limit = result.DisplayLimit, matched_count = result.PresentInBoth.Count,
        only_in_shopify_count = result.OnlyInShopify.Count, only_in_tracezilla_count = result.OnlyInTracezilla.Count,
        present_in_both = result.PresentInBoth, only_in_shopify = result.OnlyInShopify, only_in_tracezilla = result.OnlyInTracezilla
    }, new JsonSerializerOptions { WriteIndented = true }) : TableRenderer.Render(result));
    return 0;
} catch (Exception exception) {
    Console.Error.WriteLine($"Comparison failed: {exception.Message}");
    return 1;
}
