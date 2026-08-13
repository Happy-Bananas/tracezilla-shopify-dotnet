using System.Text.Json;
using TracezillaShopify.Workflows;
namespace TracezillaShopify.Tracezilla;
public sealed class TracezillaInventoryService(IJsonClient client):IInventorySource
{
 public async Task<IReadOnlyList<TracezillaInventory>> ReadWarehouseAsync(int number)
 {
  using var locationPayload=await client.GetAsync($"/location-by-number/{number}",new Dictionary<string,string>());
  var location=locationPayload.RootElement.GetProperty("data");var id=location.GetProperty("id").ToString();
  var query=new Dictionary<string,string>{{"partner_location[eq]",id},{"include","sku"},{"perPage","250"}};var result=new List<TracezillaInventory>();
  while(true){using var payload=await client.GetAsync("/inventory",query);foreach(var record in payload.RootElement.GetProperty("data").EnumerateArray()){record.TryGetProperty("sku",out var sku);var code=record.TryGetProperty("sku_code",out var direct)?direct.GetString():Text(sku,"sku_code");result.Add(new((code??throw new InvalidDataException("tracezilla inventory response is missing an SKU.")).Trim(),Number(record,"traceable_quantity_available",0),Number(record,"none_traceable_quantity_available",0),Number(sku,"default_uom_conversion",1),Number(sku,"none_traceable_uom_conversion",1)));}
   if(!payload.RootElement.TryGetProperty("links",out var links)||!links.TryGetProperty("next_page",out var next)||next.ValueKind==JsonValueKind.Null||string.IsNullOrEmpty(next.GetString()))break;
   var values=System.Web.HttpUtility.ParseQueryString(new Uri(next.GetString()!).Query);foreach(var key in values.AllKeys)if(key is not null)query[key]=values[key]!;
  }
  return result;
 }
 private static double Number(JsonElement value,string key,double fallback)=>value.ValueKind==JsonValueKind.Object&&value.TryGetProperty(key,out var field)&&field.ValueKind==JsonValueKind.Number?field.GetDouble():fallback;
 private static string? Text(JsonElement value,string key)=>value.ValueKind==JsonValueKind.Object&&value.TryGetProperty(key,out var field)?field.GetString():null;
}
