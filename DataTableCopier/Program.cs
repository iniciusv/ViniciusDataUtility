using DataUtility.DataBase.DataBases;
using DataUtility.Domain;
using System.Transactions;

namespace DataBaseCopier;
class Program
{
	static void Main(string[] args)
	{
		try
		{
			//const string sourceConnStr = "Data Source=tars.database.windows.net;Initial Catalog=ambev-poc;Persist Security Info=True;User ID=tereos;Password=WePi_r_sW&wrUNUsu9i@;Connection Timeout=10000;MultipleActiveResultSets=true;";
			//const string sourceConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=ambev-poc;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";
			const string sourceConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=sylvamo-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

			const string targetConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=ecorodovias-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

			const string TereosConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=Tereos;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

			const string SbfQAConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=slartibartfast-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

			//const string targetConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=bid-poc;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";



			// Configurações fixas
			const string tableName = "IVADefinitionPolicy";
			const string condition = "TenantID =7"; // Nova cláusula WHERE
			//const string condition = "ClientCode = 0206045996"; // Nova cláusula WHERE
			const bool replaceExisting = false;



			TransferData(
				SbfQAConnStr,
				SbfQAConnStr,
				tableName,
				condition, // Passa a condição em vez de IDs
				replaceExisting
			);

			Console.WriteLine("Transferência concluída com sucesso!");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro: {ex.Message}");
		}
	}
	static void TransferData(
		   string sourceConnStr,
		   string targetConnStr,
		   string tableName,
		   string condition,
		   bool replaceExisting)
	{
		// Extrair dados e schema da origem
		var sourceTableData = ExtractSourceData(sourceConnStr, tableName, condition);

		// Obter schema do destino
		var targetHeaders = GetTargetTableHeaders(targetConnStr, tableName);

		// Mostrar campos do target que não estão na source
		PrintMissingSourceColumns(sourceTableData.Headers, targetHeaders);

		// Criar novo SimpleTableData apenas com colunas correspondentes
		var filteredTableData = FilterTableDataByMatchingHeaders(sourceTableData, targetHeaders);

		// Inserir no destino com transação distribuída
		using (var scope = new TransactionScope())
		{
			InsertTargetData(targetConnStr, tableName, filteredTableData, condition, replaceExisting);
			scope.Complete();
		}
	}

	static void PrintMissingSourceColumns(List<string> sourceHeaders, List<string> targetHeaders)
	{
		var missingColumns = targetHeaders.Except(sourceHeaders).ToList();

		if (missingColumns.Any())
		{
			Console.WriteLine("\nCampos na tabela de destino que não estão na origem:");
			foreach (var column in missingColumns)
			{
				Console.WriteLine($"- {column}");
			}
		}
		else
		{
			Console.WriteLine("\nTodos os campos da tabela de destino estão presentes na origem.");
		}
	}

	static void RemoveColumnFromSimpleData(SimpleTableData data, string columnName)
	{
		int columnIndex = data.Headers.IndexOf(columnName);
		if (columnIndex == -1) return;

		// Remover do cabeçalho
		data.Headers.RemoveAt(columnIndex);

		// Remover das linhas de dados
		if (data.Rows != null)
		{
			foreach (var row in data.Rows)
			{
				if (row.Count > columnIndex)
				{
					row.RemoveAt(columnIndex);
				}
			}
		}

		// Remover do schema se existir
		if (data.Schema != null)
		{
			if (data.Schema.DataTypes != null && data.Schema.DataTypes.Count > columnIndex)
				data.Schema.DataTypes.RemoveAt(columnIndex);

			if (data.Schema.Nullable != null && data.Schema.Nullable.Count > columnIndex)
				data.Schema.Nullable.RemoveAt(columnIndex);

			if (data.Schema.Special != null && data.Schema.Special.Count > columnIndex)
				data.Schema.Special.RemoveAt(columnIndex);
		}

		Console.WriteLine($"\nColuna '{columnName}' removida dos dados de origem.");
	}

	static List<string> GetTargetTableHeaders(string connStr, string tableName)
	{
		using var manager = new ConnectionManager(connStr);
		using var connection = manager.GetConnection();

		var schemaLoader = new DatabaseSchemaLoader(connStr);
		var (headers, _, _) = schemaLoader.GetTableSchema(connection, tableName);

		return headers;
	}

	static SimpleTableData FilterTableDataByMatchingHeaders(SimpleTableData sourceData, List<string> targetHeaders)
	{
		var filteredData = new SimpleTableData
		{
			TableName = sourceData.TableName,
			Headers = new List<string>(),
			Rows = new List<List<string?>>(),
			Schema = sourceData.Schema != null ? new TableSchema
			{
				DataTypes = new List<string?>(),
				Nullable = new List<string?>(),
				Special = new List<string?>()
			} : null
		};

		// Encontrar índices das colunas correspondentes
		var matchingColumns = new List<(int sourceIndex, int schemaIndex, string header)>();
		for (int i = 0; i < sourceData.Headers.Count; i++)
		{
			string header = sourceData.Headers[i];
			if (targetHeaders.Contains(header))
			{
				matchingColumns.Add((i, i, header));
			}
		}

		// Adicionar cabeçalhos correspondentes
		foreach (var column in matchingColumns)
		{
			filteredData.Headers.Add(column.header);
		}

		// Adicionar dados correspondentes
		if (sourceData.Rows != null)
		{
			foreach (var row in sourceData.Rows)
			{
				var filteredRow = new List<string?>();
				foreach (var column in matchingColumns)
				{
					filteredRow.Add(row[column.sourceIndex]);
				}
				filteredData.Rows.Add(filteredRow);
			}
		}

		// Adicionar schema correspondente, se existir
		if (sourceData.Schema != null)
		{
			foreach (var column in matchingColumns)
			{
				if (sourceData.Schema.DataTypes != null && column.schemaIndex < sourceData.Schema.DataTypes.Count)
					filteredData.Schema.DataTypes.Add(sourceData.Schema.DataTypes[column.schemaIndex]);

				if (sourceData.Schema.Nullable != null && column.schemaIndex < sourceData.Schema.Nullable.Count)
					filteredData.Schema.Nullable.Add(sourceData.Schema.Nullable[column.schemaIndex]);

				if (sourceData.Schema.Special != null && column.schemaIndex < sourceData.Schema.Special.Count)
					filteredData.Schema.Special.Add(sourceData.Schema.Special[column.schemaIndex]);
			}
		}

		return filteredData;
	}

	static SimpleTableData ExtractSourceData(string connStr, string tableName, string condition)
	{
		using var manager = new ConnectionManager(connStr);
		using var connection = manager.GetConnection();

		var schemaLoader = new DatabaseSchemaLoader(connStr);
		var (headers, dataTypes, nullable) = schemaLoader.GetTableSchema(connection, tableName);

		var extractor = new DataExtractor(connection);
		var tableData = extractor.ExtractDataTable(
			tableName,
			condition: !string.IsNullOrEmpty(condition) ? condition : null
		);

		tableData.Schema = new TableSchema
		{
			DataTypes = dataTypes,
			Nullable = nullable,
			Special = new List<string?>(new string?[headers.Count])
		};

		return tableData;
	}

	static void InsertTargetData(
		string connStr,
		string tableName,
		SimpleTableData data,
		string condition,
		bool replaceExisting)
	{
		using var manager = new ConnectionManager(connStr);
		using var connection = manager.GetConnection();

		if (replaceExisting)
		{
			DataCleaner.DeleteByCondition(connection, tableName, condition);
		}

		new DataInserter(connection).InsertData(data);
	}
}