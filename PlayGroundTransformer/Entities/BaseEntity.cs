namespace ContractBid.Domain.Entities;

public class BaseEntity
{
	public long ID { get; set; }
	public string GUID { get; set; }
	public DateTime Created { get; set; }
	public string ClientCode { get; set; }

}
