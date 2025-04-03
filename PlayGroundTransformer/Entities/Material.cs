namespace ContractBid.Domain.Entities;

public class Material : BaseEntity
{
	public string Description { get; set; }
	public string? NCM { get; set; }
	public string BidGuid { get; set; }
}
