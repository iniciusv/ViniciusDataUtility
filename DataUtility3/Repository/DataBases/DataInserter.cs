using DataUtility.Domain;
using Microsoft.Data.SqlClient;
using System.Text;
namespace DataUtility.DataBase.DataBases;

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

			// Remove o ID do schema, se existir
			if (data.Schema?.DataTypes != null && data.Schema.DataTypes.Count > idIndex)
			{
				data.Schema.DataTypes.RemoveAt(idIndex);
			}
			if (data.Schema?.Nullable != null && data.Schema.Nullable.Count > idIndex)
			{
				data.Schema.Nullable.RemoveAt(idIndex);
			}
			if (data.Schema?.Special != null && data.Schema.Special.Count > idIndex)
			{
				data.Schema.Special.RemoveAt(idIndex);
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

			// Remove as colunas de data do schema, se existirem
			if (data.Schema?.DataTypes != null && data.Schema.DataTypes.Count > createdIndex)
			{
				data.Schema.DataTypes.RemoveAt(createdIndex);
			}
			if (data.Schema?.Nullable != null && data.Schema.Nullable.Count > createdIndex)
			{
				data.Schema.Nullable.RemoveAt(createdIndex);
			}
			if (data.Schema?.Special != null && data.Schema.Special.Count > createdIndex)
			{
				data.Schema.Special.RemoveAt(createdIndex);
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

			// Remove as colunas de data do schema, se existirem
			if (data.Schema?.DataTypes != null && data.Schema.DataTypes.Count > lastModifiedIndex)
			{
				data.Schema.DataTypes.RemoveAt(lastModifiedIndex);
			}
			if (data.Schema?.Nullable != null && data.Schema.Nullable.Count > lastModifiedIndex)
			{
				data.Schema.Nullable.RemoveAt(lastModifiedIndex);
			}
			if (data.Schema?.Special != null && data.Schema.Special.Count > lastModifiedIndex)
			{
				data.Schema.Special.RemoveAt(lastModifiedIndex);
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
				StringBuilder queryWithValues = new StringBuilder(query);

				for (int i = 0; i < data.Headers.Count; i++)
				{
					string columnName = data.Headers[i];
					string value = row[i];

					// Obtém o tipo de dado da coluna a partir do schema
					string dataType = data.Schema?.DataTypes?[i] ?? "nvarchar"; // Padrão para nvarchar se não houver schema

					// Converte o valor para o tipo de dado correto
					object convertedValue = ConvertValueToDataType(value, dataType);

					// Adiciona o valor ao comando SQL
					command.Parameters.AddWithValue($"@{columnName}", convertedValue ?? DBNull.Value);

					// Formata o valor para exibição na query final
					string formattedValue = FormatValueForQuery(convertedValue, dataType);
					queryWithValues.Replace($"@{columnName}", formattedValue);
				}

				// Imprime a query final com os valores substituídos
				Console.WriteLine("Query final com valores:");
				Console.WriteLine(queryWithValues.ToString());

				// Executa a query no banco de dados
				command.ExecuteNonQuery();
			}
		}
	}

	// Método auxiliar para converter valores com base no tipo de dado
	private object ConvertValueToDataType(string value, string dataType)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null; // Retorna nulo para valores vazios
		}

		try
		{
			switch (dataType.ToLower())
			{
				case "int":
					return int.Parse(value);
				case "decimal":
				case "numeric":
					return decimal.Parse(value);
				case "float":
					return float.Parse(value);
				case "datetime":
					return DateTime.Parse(value);
				case "bit":
					return bool.Parse(value) ? 1 : 0; // Booleanos são mapeados para 0 ou 1
				default:
					return value; // Strings e outros tipos são mantidos como estão
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Erro ao converter valor '{value}' para o tipo '{dataType}'.", ex);
		}
	}

	// Método auxiliar para formatar valores de acordo com o tipo de dado
	private string FormatValueForQuery(object value, string dataType)
	{
		if (value == null || value == DBNull.Value)
		{
			return "NULL"; // Valor nulo
		}

		switch (dataType.ToLower())
		{
			case "int":
			case "decimal":
			case "numeric":
			case "float":
				return value.ToString(); // Números são inseridos diretamente
			case "datetime":
				return $"'{((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss")}'"; // Datas são formatadas
			case "bit":
				return ((int)value).ToString(); // Booleanos são mapeados para 0 ou 1
			default:
				return $"'{value.ToString().Replace("'", "''")}'"; // Strings são escapadas
		}
	}
}