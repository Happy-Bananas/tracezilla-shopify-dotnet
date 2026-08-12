namespace TracezillaShopify;

public sealed record Configuration(
    string ShopifyShopUrl, string ShopifyClientId, string ShopifyClientSecret,
    string ShopifyScope, string ShopifyApiVersion, string TracezillaBaseUrl,
    string TracezillaTeamSlug, string TracezillaApiKey, TimeSpan Timeout)
{
    public static Configuration FromEnvironment()
    {
        var shop = Required("SHOPIFY_SHOP_URL").Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        if (!shop.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase) || shop.Contains('/'))
            throw new ArgumentException("SHOPIFY_SHOP_URL must look like your-store.myshopify.com.");
        if (!double.TryParse(Required("HTTP_TIMEOUT"), out var seconds) || seconds <= 0)
            throw new ArgumentException("HTTP_TIMEOUT must be a positive number.");
        return new(shop, Required("SHOPIFY_CLIENT_ID"), Required("SHOPIFY_CLIENT_SECRET"),
            Required("SHOPIFY_SCOPE"), Required("SHOPIFY_API_VERSION"), Required("TRACEZILLA_BASE_URL").TrimEnd('/'),
            Required("TRACEZILLA_TEAM_SLUG"), Required("TRACEZILLA_API_KEY"), TimeSpan.FromSeconds(seconds));
    }

    private static string Required(string key)
    {
        var value = Environment.GetEnvironmentVariable(key)?.Trim();
        return string.IsNullOrEmpty(value) ? throw new ArgumentException($"Missing required configuration: {key}") : value;
    }
}
