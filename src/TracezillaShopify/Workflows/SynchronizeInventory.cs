namespace TracezillaShopify.Workflows;

public sealed record TracezillaInventory(string Sku,double Traceable,double NonTraceable,double DefaultConversion,double NonTraceableConversion);
public sealed record ShopifyInventory(string Sku,string InventoryItemId,bool Tracked,int? Available);
public interface IInventorySource { Task<IReadOnlyList<TracezillaInventory>> ReadWarehouseAsync(int number); }
public interface IInventoryTarget { Task<IReadOnlyDictionary<string,ShopifyInventory>> ReadAtLocationAsync(string id); Task SetAvailableAsync(ShopifyInventory item,int quantity,string location); }

public sealed class SynchronizeInventory(IInventorySource source,IInventoryTarget target)
{
    public async Task<object> RunAsync(string location,int warehouse,bool dryRun=true,int limit=10)
    {
        if(string.IsNullOrWhiteSpace(location)||warehouse<1||limit<1) throw new ArgumentException("Location, warehouse, and limit must be valid.");
        var destination=await target.ReadAtLocationAsync(location); var items=new List<Dictionary<string,object?>>();
        foreach(var inventory in (await source.ReadWarehouseAsync(warehouse)).Take(limit)) {
            if(!destination.TryGetValue(inventory.Sku,out var shopify)){items.Add(Item(inventory.Sku,"skipped","No Shopify variant has this SKU."));continue;}
            if(!shopify.Tracked||shopify.Available is null){items.Add(Item(inventory.Sku,"skipped","Shopify does not track this item at the configured location."));continue;}
            try {
                var raw=inventory.Traceable*inventory.DefaultConversion+inventory.NonTraceable*inventory.NonTraceableConversion;
                if(raw<0||raw!=Math.Floor(raw)||raw>int.MaxValue) throw new InvalidDataException("Mapped quantity must be a non-negative whole number.");
                var quantity=(int)raw;
                if(quantity==shopify.Available) items.Add(Item(inventory.Sku,"unchanged",$"Quantity is already {quantity}.",quantity,quantity));
                else if(dryRun) items.Add(Item(inventory.Sku,"would_update",$"Would change quantity from {shopify.Available} to {quantity}.",shopify.Available,quantity));
                else { await target.SetAvailableAsync(shopify,quantity,location); items.Add(Item(inventory.Sku,"updated",$"Changed quantity from {shopify.Available} to {quantity}.",shopify.Available,quantity)); }
            } catch(Exception error) { items.Add(Item(inventory.Sku,"failed",error.Message)); }
        }
        int Count(string status)=>items.Count(x=>Equals(x["status"],status));
        return new {summary=new{dry_run=dryRun,updated=Count("updated"),would_update=Count("would_update"),unchanged=Count("unchanged"),skipped=Count("skipped"),failed=Count("failed")},items};
    }
    private static Dictionary<string,object?> Item(string sku,string status,string message,int? from=null,int? to=null)=>new(){{"sku",sku},{"status",status},{"message",message},{"from",from},{"to",to}};
}
