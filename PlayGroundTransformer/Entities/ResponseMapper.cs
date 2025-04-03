//using ContractBid.Domain.Entities;
//using ContractBid.Infra.Readers.Files.Models;
//using DataUtility3.Transformers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ContractBid.Domain.Mapper;

//public class ResponseMapper : HeaderMapper<Response>
//{
//	private readonly IEnumerable<Material> _materials;
//	private readonly Plant _plant;
//	private readonly Supplier _supplier;

//	public ResponseMapper(string bidGuid, IEnumerable<Material> materials,Plant plant,Supplier supplier) : base("ResponseMapper")
//	{
//		_materials = materials ?? throw new ArgumentNullException(nameof(materials));
//		_plant = plant ?? throw new ArgumentNullException(nameof(plant));
//		_supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

//		// Mapeamento dos campos diretos

//		Map("Codigo do Material", u => u.ClientCode);


//		Map("NCM", r => r.NCM);
//		Map("IPI", r => r.HasIPI);
//		Map("Líquido Unitário", r => r.NetUnitPrice);
//		Map("Bruto c Impostos", r => r.GrossUnitPrice);
	

//		// Configuração dos valores estáticos
//		SetStaticValue(r => r.Supplier, _supplier);
//		SetStaticValue(r => r.Date, DateTime.Now);

//		// Configuração do resolvedor de material
//		MapReference(
//			headerName: "Codigo do Material",
//			referenceSource: clientCode => _materials,
//			matchPredicate: (clientCode, material) =>
//				material.ClientCode.Equals(clientCode, StringComparison.OrdinalIgnoreCase)
//		);
//	}



//	public override void ApplySpecialMappings(Response model, string header, string value)
//	{
//		if (header.Equals("IPI", StringComparison.OrdinalIgnoreCase))
//		{
//			model.HasIPI = value.Equals("SIM", StringComparison.OrdinalIgnoreCase);
//		}
//	}
//}