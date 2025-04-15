using DataUtility.DataBase.DataBases;
using DataUtility.Domain;
using DataUtility.Repository.Tables;

class Program
{
	private const string ExcelFilePath = @"C:\Users\Vinicius\Downloads\Fornecedores Tereos 1.xlsx";
	private const string TereosConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=Tereos;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

	static void Main(string[] args)
	{
		Console.WriteLine("Iniciando importação de fornecedores Tereos (apenas tabela Supplier)...");

		try
		{
			// 1. Ler dados do Excel
			Console.WriteLine($"Lendo arquivo Excel: {ExcelFilePath}");
			var excelData = ExcelProcessor.LoadAndConvertExcel(ExcelFilePath);

			// Verificar se as colunas necessárias existem
			VerifyRequiredColumns(excelData);

			// 2. Processar os dados para a tabela Supplier
			var suppliersData = ProcessSupplierData(excelData);

			// 3. Inserir no banco de dados
			Console.WriteLine("Conectando ao banco de dados...");
			using (var connectionManager = new ConnectionManager(TereosConnStr))
			{
				var connection = connectionManager.GetConnection();
				var dataInserter = new DataInserter(connection);

				// Inserir fornecedores
				Console.WriteLine($"Inserindo {suppliersData.Rows.Count} fornecedores...");
				dataInserter.InsertData(suppliersData);
			}

			Console.WriteLine("Importação concluída com sucesso!");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro durante a importação: {ex.Message}");
			Console.WriteLine(ex.StackTrace);
		}

		Console.WriteLine("Pressione qualquer tecla para sair...");
		Console.ReadKey();
	}

	private static void VerifyRequiredColumns(SimpleTableData excelData)
	{
		var requiredColumns = new List<string> { "Fornecedor", "Email", "CNPJ" };
		var missingColumns = requiredColumns.Where(col => !excelData.Headers.Contains(col)).ToList();

		if (missingColumns.Any())
		{
			throw new Exception($"O arquivo Excel não contém as colunas necessárias: {string.Join(", ", missingColumns)}");
		}
	}

	private static SimpleTableData ProcessSupplierData(SimpleTableData excelData)
	{
		var supplierData = new SimpleTableData
		{
			TableName = "Supplier",
			Headers = new List<string> { "Name", "Email", "ClientCode", "Status", "IsRecommendable", "FromSupplyBrain", "Language" },
			Schema = new TableSchema
			{
				DataTypes = new List<string?> { "nvarchar", "nvarchar", "nvarchar", "int", "bit", "bit", "nvarchar" },
				Nullable = new List<string?> { "NO", "YES", "YES", "YES", "NO", "NO", "YES" }
			}
		};

		foreach (var row in excelData.Rows)
		{
			var supplierName = row[excelData.Headers.IndexOf("Fornecedor")];
			var email = row[excelData.Headers.IndexOf("Email")];
			var cnpj = row[excelData.Headers.IndexOf("CNPJ")];

			supplierData.Rows.Add(new List<string?>
		{
			supplierName?.Trim(),
			email?.Trim(),
			cnpj?.Trim(),
			"1", // Status (int)
            "true", // IsRecommendable (bit) - agora como string "true"
            "false", // FromSupplyBrain (bit) - agora como string "false"
            "pt-BR"
		});
		}
		return supplierData;
	}
}