using DataUtility.General;
using DataUtility2.General;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility2.DataBeseUtils;


public class DatabaseSchemaLoader
{
	private readonly string _connectionString;

	public DatabaseSchemaLoader(string connectionString)
	{
		_connectionString = connectionString;
	}

	public IEnumerable<SimpleTableData> GetAllTableSchemas()
	{
		var tables = new List<SimpleTableData>();

		using (var connection = new SqlConnection(_connectionString))
		{
			connection.Open();

			// Obtém todos os nomes de tabelas no banco de dados
			var schemaTable = connection.GetSchema("Tables");
			foreach (DataRow row in schemaTable.Rows)
			{
				var tableName = row["TABLE_NAME"].ToString();
				var tableSchema = GetTableSchema(connection, tableName);
				tables.Add(new SimpleTableData
				{
					TableName = tableName,
					Headers = tableSchema.Item1,
					Schema = new TableSchema
					{
						DataTypes = tableSchema.Item2,
						Nullable = tableSchema.Item3,
						Special = new List<string?>(new string?[tableSchema.Item1.Count]) // Inicializa uma lista de valores especiais como nulos
					},
					Rows = new List<List<string?>>() // Inicializa a lista de linhas como vazia
				});
			}
		}

		return tables;
	}

	private (List<string>, List<string?>, List<string?>) GetTableSchema(SqlConnection connection, string tableName)
	{
		var headers = new List<string>();
		var dataTypes = new List<string?>();
		var nullable = new List<string?>();

		using (var command = new SqlCommand($"SELECT * FROM {tableName} WHERE 1 = 0", connection)) // Consulta para obter o schema sem dados
		{
			using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
			{
				var dataTable = reader.GetSchemaTable();
				foreach (DataRow column in dataTable.Rows)
				{
					headers.Add(column["ColumnName"].ToString());
					dataTypes.Add(column["DataType"].ToString());
					nullable.Add(column["AllowDBNull"].ToString());
				}
			}
		}

		return (headers, dataTypes, nullable);
	}
}