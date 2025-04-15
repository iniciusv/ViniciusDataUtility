using DataUtility.DataBase.DataBases;
using DataUtility.Domain;
using System.Transactions;

namespace DataBaseCopier;

public class DatabaseCopierService
{
	public void CopyDataBetweenDatabases(
		string sourceConnStr,
		string targetConnStr,
		string tableName,
		string condition,
		bool replaceExisting)
	{
		try
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

			Console.WriteLine("Transferência concluída com sucesso!");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro: {ex.Message}");
			throw; // Re-throw para permitir tratamento adicional se necessário
		}
	}

	public void PrintMissingSourceColumns(List<string> sourceHeaders, List<string> targetHeaders)
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

	public List<string> GetTargetTableHeaders(string connStr, string tableName)
	{
		using var manager = new ConnectionManager(connStr);
		using var connection = manager.GetConnection();

		var schemaLoader = new DatabaseSchemaLoader(connStr);
		var (headers, _, _) = schemaLoader.GetTableSchema(connection, tableName);

		return headers;
	}

	public SimpleTableData FilterTableDataByMatchingHeaders(SimpleTableData sourceData, List<string> targetHeaders)
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

	public SimpleTableData ExtractSourceData(string connStr, string tableName, string condition)
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

	public void InsertTargetData(
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
