using DataUtility.Domain;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataUtility.DataBase.DataBases;

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

	public (List<string>, List<string>, List<string?>) GetTableSchema(SqlConnection connection, string tableName)
	{
		var headers = new List<string>();
		var dataTypes = new List<string>();
		var nullable = new List<string?>();

		// Consulta para obter os tipos de dados do SQL Server
		string query = @"
        SELECT 
            c.name AS ColumnName,
            t.name AS DataType,
            c.is_nullable
        FROM 
            sys.columns c
        INNER JOIN 
            sys.types t ON c.user_type_id = t.user_type_id
        WHERE 
            c.object_id = OBJECT_ID(@TableName)";

		using (var cmd = new SqlCommand(query, connection))
		{
			cmd.Parameters.AddWithValue("@TableName", tableName);
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					headers.Add(reader["ColumnName"].ToString());
					dataTypes.Add(reader["DataType"].ToString().ToLower()); // Ex: "decimal", "int"
					nullable.Add(reader["is_nullable"].ToString());
				}
			}
		}

		return (headers, dataTypes, nullable);
	}
}