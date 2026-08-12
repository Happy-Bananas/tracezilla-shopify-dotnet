namespace TracezillaShopify.Shopify;

public static class GetProductVariants
{
    public const string Document = """
        query GetProductVariants($first: Int!, $after: String) {
          productVariants(first: $first, after: $after) {
            nodes { id sku displayName }
            pageInfo { hasNextPage endCursor }
          }
        }
        """;
}
