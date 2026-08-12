using TracezillaShopify.Shared;
using TracezillaShopify.Workflows;

namespace TracezillaShopify.Tests;

public sealed class CompareCatalogsTests
{
    [Fact]
    public async Task ComparesCompleteCatalogs()
    {
        var result = await new CompareCatalogs(new FakeReader("B", "A"), new FakeReader("A", "C")).RunAsync();
        Assert.Equal(["A"], result.PresentInBoth);
        Assert.Equal(["B"], result.OnlyInShopify);
        Assert.Equal(["C"], result.OnlyInTracezilla);
        Assert.Equal("differences", result.Status);
    }

    private sealed class FakeReader(params string[] skus) : ICatalogReader
    {
        public Task<IReadOnlyList<CatalogItem>> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogItem>>(skus.Select(sku => new CatalogItem(sku, sku)).ToArray());
    }
}
