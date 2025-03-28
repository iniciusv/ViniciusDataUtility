using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseCopier;
public static class DataCleaner
{
	public static void DeleteByIds(
		SqlConnection connection,
		string tableName,
		IEnumerable<string> ids)
	{
		if (!ids?.Any() ?? true) return;

		var commandText = $@"
                DELETE FROM [{tableName}] 
                WHERE ID IN ({string.Join(",", ids.Select((_, i) => $"@id{i}"))})";

		using var cmd = new SqlCommand(commandText, connection);

		// Adiciona parâmetros de forma segura
		var parameters = ids.Select((id, i) =>
			new SqlParameter($"@id{i}", id)).ToArray();

		cmd.Parameters.AddRange(parameters);
		cmd.ExecuteNonQuery();
	}
	public static void DeleteByCondition(SqlConnection connection, string tableName, string condition)
	{
		string query = $"DELETE FROM [{tableName}] WHERE {condition}";
		using (var cmd = new SqlCommand(query, connection))
		{
			cmd.ExecuteNonQuery();
		}
	}
}