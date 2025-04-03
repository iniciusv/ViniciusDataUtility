using ContractBid.Domain.Enums;

namespace ContractBid.Domain.Entities;

public class Request
{
	public Material Material { get; set; }
	public Plant Plant { get; set; }
	public string PlantMaterialKey => $"{Material.ID}-{Plant.ID}";
	public MaterialOriginType MaterialOrigin { get; set; }

}
