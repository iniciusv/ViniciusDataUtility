using System.ComponentModel;

namespace ContractBid.Domain.Enums;

public enum MaterialOriginType
{
	[Description("NACIONAL")]
	National = 0,

	[Description("IMPORTADO")]
	Imported = 1
}
