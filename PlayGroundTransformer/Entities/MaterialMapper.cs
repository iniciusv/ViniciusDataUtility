using ContractBid.Domain.Entities;
using DataUtility3.Transformers;

namespace ContractBid.Domain.Mapper;

public class MaterialMapper : HeaderMapper<Material>
{
	public MaterialMapper(string bidGuid) : base("MaterialMapper")
	{
		Map("Codigo do Material", u => u.ClientCode);
		Map("Descrição Material", u => u.Description);
		Map("NCM", u => u.NCM);

		SetStaticValue(u => u.BidGuid, bidGuid);
		SetStaticValue(u => u.GUID, Guid.NewGuid().ToString());
		SetStaticValue(u => u.Created, DateTime.UtcNow);
	}
}