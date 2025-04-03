// Mapeador para Response
using ContractBid.Domain.Entities;
using ContractBid.Domain.Enums;
using ContractBid.Domain.Mapper;
using ContractBid.Infra.Readers.Files.Models.Validatiors;
using DataUtility.Repository.Tables;
using DataUtility3.Repository.Tables;
using DataUtility3.Transformers;
using System.Globalization;
using System.Text;

public class ResponseMapper : HeaderMapper<Response>
{
	private readonly List<Material> _materials;
	private readonly string _supplierClientCode;
	private readonly string _supplierCNPJ;
	private readonly string _bidGuid;

	public ResponseMapper(List<Material> materials, string supplierClientCode, string supplierCNPJ, string bidGuid) : base("ResponseMapper")
	{
		_materials = materials;
		_supplierClientCode = supplierClientCode;
		_supplierCNPJ = supplierCNPJ;
		_bidGuid = bidGuid;

		// Mapeamento dos campos
		Map("Codigo do Material", r => r.Request.Material.ClientCode); // Chave de relacionamento
		Map("Preço Unitário Bruto", r => r.GrossUnitPrice);
		Map("Preço Unitário Líquido", r => r.NetUnitPrice);
		Map("Possui IPI", r => r.HasIPI);
		Map("Data Cotação", r => r.Date);
		Map("NCM", r => r.NCM);

		SetStaticValue(r => r.BidGuid, bidGuid);
		SetStaticValue(r => r.GUID, Guid.NewGuid().ToString());
		SetStaticValue(r => r.Created, DateTime.UtcNow);
	}

	public override Response CreateInstance()
	{
		// Cria o Supplier dentro do CreateInstance
		var supplier = new Supplier()
		{
			ClientCode = _supplierClientCode,
			CNPJ = _supplierCNPJ,
			State = BrazilStates.SP,
			Type = BidParticipantType.Supplier,
			GUID = Guid.NewGuid().ToString(),
			Created = DateTime.UtcNow,
		};

		return new Response()
		{
			Supplier = supplier,
			Request = new Request(),
			GUID = Guid.NewGuid().ToString(),
			Created = DateTime.UtcNow,
			BidGuid = _bidGuid
		};
	}

	public override void ApplySpecialMappings(Response model, string header, string value)
	{
		if (header.Equals("Codigo do Material", StringComparison.OrdinalIgnoreCase))
		{
			var material = _materials.FirstOrDefault(m => m.ClientCode == value);
			if (material != null)
			{
				model.Request.Material = material;
			}
		}

		if (header.Equals("Data Cotação", StringComparison.OrdinalIgnoreCase))
		{
			if (DateTime.TryParseExact(value, "dd/MM/yyyy",
				CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				model.Date = date;
			}
		}

		if (header.Equals("IPI", StringComparison.OrdinalIgnoreCase))
		{
			model.HasIPI = value.Equals("SIM", StringComparison.OrdinalIgnoreCase);
		}
	}
}


// Modificação no Program
class Program
{
	static void Main(string[] args)
	{
		var BidTemplate = @"C:\Config\Thereos\Template MRO Ind - Ferramentas.xlsx";
		var Exemplo1 = @"C:\Config\Thereos\Exemplo1.xlsx";

		var bidData = ExcelProcessor.LoadAndConvertExcel(BidTemplate, 9);
		var responseData = ExcelProcessor.LoadAndConvertExcel(Exemplo1, 9);

		string bidGuid = "Thereos MRO";

		var readerConfig = new ReaderConfig(
			encoding: Encoding.UTF8,
			dateFormat: "dd/MM/yyyy",
			decimalSeparator: ",",
			numberDecimalDigits: 2);

		// 1. Carregar Materiais
		var materialBinder = new ModelBinder<Material>(
			new MaterialMapper(bidGuid),
			new MaterialValidator(),
			readerConfig);

		var (validMaterials, materialLineResults) = materialBinder.Bind(bidData);

		// Atribuir IDs incrementais
		long idCounter = 1;
		foreach (var material in validMaterials)
		{
			material.ID = idCounter++;
		}



		// 3. Carregar Respostas (relacionando com Materiais e Fornecedores)
		var responseBinder = new ModelBinder<Response>(new ResponseMapper(validMaterials, "ex1", "ex1", bidGuid),	new BidTemplateValidator(),	readerConfig);

		var (validResponses, responseLineResults) = responseBinder.Bind(responseData);


		Console.WriteLine($"Materiais válidos: {validMaterials.Count}");

		Console.WriteLine($"Respostas válidas: {validResponses.Count}");
	}
}