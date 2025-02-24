using DataUtility.General;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility2.DataBeseUtils;

public class DataInserter
{
	private readonly SqlConnection _connection;

	public DataInserter(SqlConnection connection)
	{
		_connection = connection;
	}

	public void InsertData(SimpleTableData data)
	{
		if (data.Rows == null || data.Rows.Count == 0)
		{
			Console.WriteLine("Nenhum dado para inserir.");
			return;
		}

		// Remove a coluna ID da lista de cabeçalhos (se existir)
		int idIndex = data.Headers.IndexOf("ID");
		if (idIndex != -1)
		{
			data.Headers.RemoveAt(idIndex);
			foreach (var row in data.Rows)
			{
				row.RemoveAt(idIndex);
			}
		}

		// Remove as colunas de data da lista de cabeçalhos (se existirem)
		int createdIndex = data.Headers.IndexOf("Created");
		if (createdIndex != -1)
		{
			data.Headers.RemoveAt(createdIndex);
			foreach (var row in data.Rows)
			{
				row.RemoveAt(createdIndex);
			}
		}

		int lastModifiedIndex = data.Headers.IndexOf("LastModifield");
		if (lastModifiedIndex != -1)
		{
			data.Headers.RemoveAt(lastModifiedIndex);
			foreach (var row in data.Rows)
			{
				row.RemoveAt(lastModifiedIndex);
			}
		}

		// Constrói a query de inserção
		string columns = string.Join(", ", data.Headers.Select(header => $"[{header}]"));
		string values = string.Join(", ", data.Headers.Select(header => $"@{header}"));

		// Adiciona as colunas de data com GETDATE()
		columns += ", [Created], [LastModifield]";
		values += ", GETDATE(), GETDATE()";

		string query = $"INSERT INTO [{data.TableName}] ({columns}) VALUES ({values})";

		using (var command = new SqlCommand(query, _connection))
		{
			foreach (var row in data.Rows)
			{
				command.Parameters.Clear();
				for (int i = 0; i < data.Headers.Count; i++)
				{
					string columnName = data.Headers[i];
					string value = row[i];

					// Para outras colunas, insere o valor diretamente
					command.Parameters.AddWithValue($"@{columnName}", value ?? (object)DBNull.Value);
				}
				command.ExecuteNonQuery();
			}
		}
	}
}