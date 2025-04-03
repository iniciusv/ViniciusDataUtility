using ContractBid.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace ContractBid.Domain.Entities;

public class Supplier : BaseEntity
{
	public string CNPJ { get; set; }
	public BrazilStates State { get; set; }
	public BidParticipantType Type { get; set; }
	public string Name { get; set; }

}
