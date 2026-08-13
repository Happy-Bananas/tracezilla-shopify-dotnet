using System.Text.Json;
using TracezillaShopify;
using TracezillaShopify.Output;
using TracezillaShopify.Shopify;
using TracezillaShopify.Tracezilla;
using TracezillaShopify.Workflows;

try {
    var createSkus = args.Contains("create-tracezilla-skus");
    var json = args.Contains("--json");
    var limitArgument = args.FirstOrDefault(value => value.StartsWith("--limit=", StringComparison.Ordinal));
    var limit = limitArgument is null ? 10 : int.Parse(limitArgument[8..]);
    var configuration = Configuration.FromEnvironment();
    if (args.Contains("list-shopify-locations")) {
        var locations=await new ShopifyLocationService(new ShopifyClient(configuration)).ReadAsync();
        if(json) Console.WriteLine(JsonSerializer.Serialize(new{count=locations.Count,locations=locations.Select(x=>new{graph_ql_id=x.GraphQlId,legacy_id=x.LegacyId,name=x.Name,is_active=x.IsActive,has_active_inventory=x.HasActiveInventory,fulfills_online_orders=x.FulfillsOnlineOrders,address=new{address1=x.Address.Address1,address2=x.Address.Address2,city=x.Address.City,province=x.Address.Province,country=x.Address.Country,zip=x.Address.Zip}})},new JsonSerializerOptions{WriteIndented=true}));
        else { Console.WriteLine($"{"Name",-24} {"Status",-9} {"Inventory",-10} {"Online orders",-13} {"Legacy ID",-22} GraphQL ID"); Console.WriteLine(new string('-',112)); foreach(var x in locations){Console.WriteLine($"{x.Name,-24} {(x.IsActive?"Active":"Inactive"),-9} {(x.HasActiveInventory?"Yes":"No"),-10} {(x.FulfillsOnlineOrders?"Yes":"No"),-13} {x.LegacyId,-22} {x.GraphQlId}");var a=x.Address;var address=string.Join(", ",new[]{a.Address1,a.Address2,string.Join(" ",new[]{a.Zip,a.City}.Where(v=>!string.IsNullOrWhiteSpace(v))),a.Province,a.Country}.Where(v=>!string.IsNullOrWhiteSpace(v)));Console.WriteLine($"Address: {(address.Length>0?address:"—")}");} Console.WriteLine($"\n{locations.Count} location(s) returned.");if(locations.Count==0)Console.WriteLine("No Shopify locations are available to this app.");} return 0;
    }
    var shopify = new ShopifyCatalogService(new ShopifyClient(configuration), new ShopifyVariantMapper());
    var tracezilla = new TracezillaCatalogService(new TracezillaClient(configuration), new TracezillaSkuMapper());
    if (createSkus) {
        var execute=args.Contains("--execute"); if(execute&&!args.Contains("--confirm")) throw new ArgumentException("Execution requires both --execute and --confirm.");
        var creation=await new CreateTracezillaSkus(shopify,tracezilla).RunAsync(!execute,limit); var outputItems=creation.Items.Select(x=>new{source_id=x.SourceId,sku=x.Sku,status=x.Status,message=x.Message}); Console.WriteLine(JsonSerializer.Serialize(new{summary=creation.Summary,items=outputItems},new JsonSerializerOptions{WriteIndented=true})); return creation.FailedCount>0?1:0;
    }
    var result = await new CompareCatalogs(shopify, tracezilla).RunAsync(limit);
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
