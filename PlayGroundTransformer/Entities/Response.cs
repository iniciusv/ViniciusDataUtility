using ContractBid.Domain.Enums;

namespace ContractBid.Domain.Entities;

public class Response : BaseEntity
{
	public Request Request { get; set; }
	public Supplier Supplier { get; set; }
	public decimal GrossUnitPrice { get; set; }
	public decimal NetUnitPrice { get; set; }
	public decimal Price => NetUnitPrice;
	public bool HasIPI { get; set; }
	public DateTime Date { get; set; }
	public string BidGuid { get; set; }
	public decimal Saving { get; set; } = 0;
	public string? NCM { get; set; }

	public void SetSaving(decimal benchmark)
	{
		Saving = Price == 0 ? 0 : Math.Max(benchmark - Price, 0);
	}
}
