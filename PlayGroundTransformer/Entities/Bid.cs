namespace ContractBid.Domain.Entities;

public class Bid : BaseEntity
{
	public string Name { get; set; }
	public DateTime? Finished { get; set; }
}
