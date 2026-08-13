namespace TracezillaShopify.Tests;
public sealed class CreateTracezillaSkusTests { [Fact] public void MappingAssumptionsAreExplicit(){var payload=new{sku_code="BANANA-001",global_name="BANANA-001",weight_factor_net=1.0,weight_factor_gross=1.0,unit_of_measure="pcs",lot_unit="colli",default_uom_conversion=1.0};Assert.Equal("pcs",payload.unit_of_measure);Assert.Equal("colli",payload.lot_unit);} }
