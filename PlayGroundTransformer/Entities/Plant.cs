using ContractBid.Domain.Enums;

namespace ContractBid.Domain.Entities;

public class Plant : BaseEntity
{
	public string CNPJ { get; set; }
	public BrazilStates State { get; set; }
	public BidParticipantType Type { get; set; }
	public string Name { get; set; }

}
