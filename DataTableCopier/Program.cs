//using DataUtility.DataBase.DataBases;
//using DataUtility.Domain;
//using System.Transactions;

//namespace DataBaseCopier;
//class Program
//{
//	static void Main(string[] args)
//	{
//		try
//		{
//			// Configurações fixas
//			const string sourceConnStr = "Data Source=tars.database.windows.net;Initial Catalog=Slartibartfast-qa-2;Persist Security Info=True;User ID=SFB;Password=Slarti@142536@*;Connection Timeout=100;MultipleActiveResultSets=true;TrustServerCertificate=true";
//			const string targetConnStr = "Data Source=tars.database.windows.net;Initial Catalog=Slartibartfast-dev;Persist Security Info=True;User ID=SFB;Password=Slarti@142536@*;Connection Timeout=100;MultipleActiveResultSets=true;TrustServerCertificate=true";
//			const string tableName = "EnvelopeOpenerPolicy";
//			const string condition = "TenantID = 1"; // Nova cláusula WHERE
//			const bool replaceExisting = true;

//			TransferData(
//				sourceConnStr,
//				targetConnStr,
//				tableName,
//				condition, // Passa a condição em vez de IDs
//				replaceExisting
//			);

//			Console.WriteLine("Transferência concluída com sucesso!");
//		}
//		catch (Exception ex)
//		{
//			Console.WriteLine($"Erro: {ex.Message}");
//		}
//	}
//	static void TransferData(
//		string sourceConnStr,
//		string targetConnStr,
//		string tableName,
//		string condition, // Alterado para condição
//		bool replaceExisting)
//	{
//		// Extrair dados e schema
//		var tableData = ExtractSourceData(sourceConnStr, tableName, condition);

//		// Inserir no destino com transação distribuída
//		using (var scope = new TransactionScope())
//		{
//			//InsertTargetData(targetConnStr, tableName, tableData, condition, replaceExisting);
//			scope.Complete();
//		}
//	}

//	static SimpleTableData ExtractSourceData(string connStr, string tableName, string condition)
//	{
//		using var manager = new ConnectionManager(connStr);
//		using var connection = manager.GetConnection();

//		// Extrair o schema da tabela
//		var schemaLoader = new DatabaseSchemaLoader(connStr);
//		var (headers, dataTypes, nullable) = schemaLoader.GetTableSchema(connection, tableName);

//		// Extrair os dados da tabela
//		var extractor = new DataExtractor(connection);
//		var tableData = extractor.ExtractDataTable(
//			tableName,
//			condition: !string.IsNullOrEmpty(condition) ? condition : null // Usa a condição diretamente
//		);

//		// Associar o schema aos dados extraídos
//		tableData.Schema = new TableSchema
//		{
//			DataTypes = dataTypes,
//			Nullable = nullable,
//			Special = new List<string?>(new string?[headers.Count])
//		};

//		return tableData;
//	}

//	static void InsertTargetData(
//	string connStr,
//	string tableName,
//	SimpleTableData data,
//	string condition, // Adicionado parâmetro de condição
//	bool replaceExisting)
//	{
//		using var manager = new ConnectionManager(connStr);
//		using var connection = manager.GetConnection();

//		if (replaceExisting)
//		{
//			DataCleaner.DeleteByCondition(connection, tableName, condition); // Novo método de deleção
//		}

//		new DataInserter(connection).InsertData(data);
//	}
//}