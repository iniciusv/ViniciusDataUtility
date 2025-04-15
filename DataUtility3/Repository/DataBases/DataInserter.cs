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

	public void InsertData(SimpleTableData data, bool substituteCreation = true, bool substituteLastModified = true, bool executeInsert = false)
	{
		if (data.Rows == null || data.Rows.Count == 0)
		{
			Console.WriteLine("Nenhum dado para inserir.");
			return;
		}

		bool hasCreatedColumn = data.Headers.Contains("Created");
		bool hasLastModifiedColumn = data.Headers.Contains("LastModifield");

		// Remove ID column if exists
		int idIndex = data.Headers.IndexOf("ID");
		if (idIndex != -1)
		{
			RemoveColumn(data, idIndex);
		}

		// Remove date columns only if we're going to substitute them
		if (hasCreatedColumn && substituteCreation)
		{
			int createdIndex = data.Headers.IndexOf("Created");
			if (createdIndex != -1) RemoveColumn(data, createdIndex);
		}

		if (hasLastModifiedColumn && substituteLastModified)
		{
			int lastModifiedIndex = data.Headers.IndexOf("LastModifield");
			if (lastModifiedIndex != -1) RemoveColumn(data, lastModifiedIndex);
		}

		// Build the insert query
		var columns = new List<string>(data.Headers.Select(header => $"[{header}]"));
		var values = new List<string>(data.Headers.Select(header => $"@{header}"));

		if (hasCreatedColumn)
		{
			columns.Add("[Created]");
			values.Add(substituteCreation ? "GETDATE()" : "@Created");
		}

		if (hasLastModifiedColumn)
		{
			columns.Add("[LastModifield]");
			values.Add(substituteLastModified ? "GETDATE()" : "@LastModifield");
		}

		string query = $"INSERT INTO [{data.TableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

		using (var command = new SqlCommand(query, _connection))
		{
			foreach (var row in data.Rows)
			{
				command.Parameters.Clear();
				var formattedQuery = BuildFormattedQuery(data, hasCreatedColumn, hasLastModifiedColumn,
														 substituteCreation, substituteLastModified);

				// Add parameters for regular columns
				for (int i = 0; i < data.Headers.Count; i++)
				{
					string columnName = data.Headers[i];
					string value = row[i];
					string dataType = data.Schema?.DataTypes?[i] ?? "nvarchar";
					object convertedValue = ConvertValueToDataType(value, dataType);

					command.Parameters.AddWithValue($"@{columnName}", convertedValue != null ? convertedValue : DBNull.Value);
				}

				// Add parameters for date columns if not substituted
				if (hasCreatedColumn && !substituteCreation)
				{
					int createdIndex = data.Headers.IndexOf("Created");
					object createdValue = createdIndex != -1 ? (object)row[createdIndex] : DBNull.Value;
					command.Parameters.AddWithValue("@Created", createdValue);
				}

				if (hasLastModifiedColumn && !substituteLastModified)
				{
					int lastModifiedIndex = data.Headers.IndexOf("LastModifield");
					object lastModifiedValue = lastModifiedIndex != -1 ? (object)row[lastModifiedIndex] : DBNull.Value;
					command.Parameters.AddWithValue("@LastModifield", lastModifiedValue);
				}

				Console.WriteLine("\nQuery formatada:");
				Console.WriteLine(formattedQuery.ToString());
				if (executeInsert)
				{
					command.ExecuteNonQuery();
					Console.WriteLine("Dados inseridos com sucesso!");
				}
			}
		}
	}

	private void RemoveColumn(SimpleTableData data, int index)
	{
		data.Headers.RemoveAt(index);
		foreach (var row in data.Rows) row.RemoveAt(index);

		if (data.Schema?.DataTypes != null && data.Schema.DataTypes.Count > index)
			data.Schema.DataTypes.RemoveAt(index);
		if (data.Schema?.Nullable != null && data.Schema.Nullable.Count > index)
			data.Schema.Nullable.RemoveAt(index);
		if (data.Schema?.Special != null && data.Schema.Special.Count > index)
			data.Schema.Special.RemoveAt(index);
	}

	private StringBuilder BuildFormattedQuery(SimpleTableData data, bool hasCreatedColumn, bool hasLastModifiedColumn,
											bool substituteCreation, bool substituteLastModified)
	{
		var formattedQuery = new StringBuilder();
		formattedQuery.AppendLine($"INSERT INTO [{data.TableName}]");
		formattedQuery.AppendLine("(");

		// Add regular columns
		for (int i = 0; i < data.Headers.Count; i++)
		{
			string comma = i < data.Headers.Count - 1 || hasCreatedColumn || hasLastModifiedColumn ? "," : "";
			formattedQuery.AppendLine($"    [{data.Headers[i]}]{comma}");
		}

		// Add date columns if they exist
		if (hasCreatedColumn)
		{
			string comma = hasLastModifiedColumn ? "," : "";
			formattedQuery.AppendLine($"    [Created]{comma}");
		}
		if (hasLastModifiedColumn)
		{
			formattedQuery.AppendLine("    [LastModifield]");
		}

		formattedQuery.AppendLine(")");
		formattedQuery.AppendLine("VALUES");
		formattedQuery.AppendLine("(");

		// Add values for regular columns
		for (int i = 0; i < data.Headers.Count; i++)
		{
			string columnName = data.Headers[i];
			string value = data.Rows[0][i]; // Using first row for demonstration
			string dataType = data.Schema?.DataTypes?[i] ?? "nvarchar";
			object convertedValue = ConvertValueToDataType(value, dataType);
			string formattedValue = FormatValueForQuery(convertedValue, dataType);

			string comma = i < data.Headers.Count - 1 || hasCreatedColumn || hasLastModifiedColumn ? "," : "";
			formattedQuery.AppendLine($"    {formattedValue}{comma} -- [{columnName}]");
		}

		// Add values for date columns
		if (hasCreatedColumn)
		{
			string comma = hasLastModifiedColumn ? "," : "";
			string createdValue = substituteCreation ? "GETDATE()" :
				FormatValueForQuery(data.Rows[0].Count > data.Headers.Count ?
				data.Rows[0][data.Headers.Count] : null, "datetime");
			formattedQuery.AppendLine($"    {createdValue}{comma} -- [Created]");
		}

		if (hasLastModifiedColumn)
		{
			string lastModifiedValue = substituteLastModified ? "GETDATE()" :
				FormatValueForQuery(data.Rows[0].Count > data.Headers.Count + 1 ?
				data.Rows[0][data.Headers.Count + 1] : null, "datetime");
			formattedQuery.AppendLine($"    {lastModifiedValue} -- [LastModifield]");
		}

		formattedQuery.AppendLine(");");
		return formattedQuery;
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
				// Garante que números decimais usem ponto como separador
				return value.ToString().Replace(',', '.');
			case "datetime":
				return $"'{((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss")}'"; // Datas são formatadas
			case "bit":
				return ((int)value).ToString(); // Booleanos são mapeados para 0 ou 1
			default:
				return $"'{value.ToString().Replace("'", "''")}'"; // Strings são escapadas
		}
	}
}