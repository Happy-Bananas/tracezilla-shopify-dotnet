namespace TracezillaShopify.Shared;

public sealed record CatalogItem(string Sku, string SourceId, string? Name = null);

public interface ICatalogReader
{
    Task<IReadOnlyList<CatalogItem>> ReadAsync(CancellationToken cancellationToken = default);
}
