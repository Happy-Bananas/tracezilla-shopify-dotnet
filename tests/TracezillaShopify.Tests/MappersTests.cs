using System.Text.Json;
using TracezillaShopify.Shopify;
using TracezillaShopify.Tracezilla;

namespace TracezillaShopify.Tests;

public sealed class MappersTests
{
    [Fact]
    public void ShopifyMapperNormalizesAndSkipsBlankSku()
    {
        using var valid = JsonDocument.Parse("""{"id":"1","sku":" BANANA-001 "}""");
        using var blank = JsonDocument.Parse("""{"id":"2","sku":" "}""");
        var mapper = new ShopifyVariantMapper();
        Assert.Equal("BANANA-001", mapper.Map(valid.RootElement)?.Sku);
        Assert.Null(mapper.Map(blank.RootElement));
    }

    [Fact]
    public void TracezillaMapperNormalizesSku()
    {
        using var payload = JsonDocument.Parse("""{"id":42,"sku_code":" BANANA-001 "}""");
        Assert.Equal(("BANANA-001", "42"), (new TracezillaSkuMapper().Map(payload.RootElement)?.Sku, new TracezillaSkuMapper().Map(payload.RootElement)?.SourceId));
    }
}
