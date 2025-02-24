using DataUtility.Domain;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
namespace DataUtility.DataBase.DataBases;

public class DataExtractor
{
	private readonly SqlConnection _connection;

	public DataExtractor(SqlConnection connection)
	{
		_connection = connection;
	}

	// Adicionando um parâmetro 'condition' opcional
	public SimpleTableData ExtractDataTable(string tableName, List<string>? columns = null, string? condition = null)
	{
		var data = new SimpleTableData
		{
			TableName = tableName
		};

		// Constrói a string de seleção de colunas ou usa '*' se nenhuma coluna for especificada
		string columnPart = columns != null && columns.Count > 0 ? string.Join(", ", columns.Select(col => $"[{col}]")) : "*";

		// Constrói a query SQL com a condição, se fornecida
		string query = $"SELECT {columnPart} FROM [{tableName}]";
		if (!string.IsNullOrWhiteSpace(condition))
		{
			query += $" WHERE {condition}";
		}

		SqlCommand command = new SqlCommand(query, _connection);
		using (var reader = command.ExecuteReader())
		{
			// Adiciona cabeçalhos com base no que foi lido do banco ou das colunas solicitadas
			if (columns != null && columns.Count > 0)
			{
				data.Headers.AddRange(columns);
			}
			else
			{
				for (int i = 0; i < reader.FieldCount; i++)
				{
					data.Headers.Add(reader.GetName(i));
				}
			}

			// Lê cada linha e adiciona à lista de linhas
			while (reader.Read())
			{
				var row = new List<string?>();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					// Trata valores nulos de forma segura
					row.Add(reader.IsDBNull(i) ? null : reader[i].ToString());
				}
				data.Rows.Add(row);
			}
		}
		return data;
	}

}
