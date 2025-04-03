using ContractBid.Domain.Enums;

namespace ContractBid.Domain.Entities;

public class BidParticipant : BaseNamedClass
{
	public string ClientCode { get; set; }
	public string CNPJ { get; set; }
	public BrazilStates State { get; set; }
	public BidParticipantType Type { get; set; }
}
