using System;
using System.Collections.Generic;
using System.Data;
using DataUtility.Domain;
using Microsoft.Data.SqlClient;

namespace DataUtility.DataBase.DataBases;

public class SchemaGetter
{
	private readonly SqlConnection _connection;

	public SchemaGetter(SqlConnection connection)
	{
		_connection = connection;
	}

	public List<TableSchema> GetTableSchemas(List<string> tableNames)
	{
		List<TableSchema> schemas = new List<TableSchema>();
		DataTable allTables = _connection.GetSchema("Tables");

		foreach (DataRow row in allTables.Rows)
		{
			string fullTableName = $"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}";

			if (tableNames.Contains(fullTableName))
			{
				var queryColumns = $@"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = '{row["TABLE_SCHEMA"]}' AND TABLE_NAME = '{row["TABLE_NAME"]}'";

				var queryForeignKeys = $@"
                    SELECT 
                        kcu.COLUMN_NAME,
                        rc.CONSTRAINT_SCHEMA + '.' + rc.UNIQUE_CONSTRAINT_NAME AS FOREIGN_TABLE,
                        kcu2.COLUMN_NAME AS FOREIGN_COLUMN
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                        ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu2
                        ON rc.UNIQUE_CONSTRAINT_NAME = kcu2.CONSTRAINT_NAME
                    WHERE kcu.TABLE_SCHEMA = '{row["TABLE_SCHEMA"]}' AND kcu.TABLE_NAME = '{row["TABLE_NAME"]}'";

				SqlCommand command = new SqlCommand(queryColumns, _connection);
				SqlDataAdapter adapter = new SqlDataAdapter(command);
				DataTable columnsTable = new DataTable();
				adapter.Fill(columnsTable);

				SqlCommand commandFk = new SqlCommand(queryForeignKeys, _connection);
				SqlDataAdapter adapterFk = new SqlDataAdapter(commandFk);
				DataTable fkTable = new DataTable();
				adapterFk.Fill(fkTable);

				List<string> columnNames = new List<string>();
				List<string> dataTypes = new List<string>();
				List<string> nullable = new List<string>();
				List<string> special = new List<string>();

				foreach (DataRow col in columnsTable.Rows)
				{
					columnNames.Add(col["COLUMN_NAME"].ToString());
					dataTypes.Add(col["DATA_TYPE"].ToString());
					nullable.Add(col["IS_NULLABLE"].ToString() == "YES" ? "nullable" : "not nullable");
					special.Add(""); // Start with an empty special info

					foreach (DataRow fk in fkTable.Rows)
					{
						if (col["COLUMN_NAME"].ToString() == fk["COLUMN_NAME"].ToString())
						{
							special[special.Count - 1] += $"FK to {fk["FOREIGN_TABLE"]}.{fk["FOREIGN_COLUMN"]}; ";
						}
					}
				}

				schemas.Add(new TableSchema
				{
					DataTypes = dataTypes,
					Nullable = nullable,
					Special = special
				});
			}
		}
		return schemas;
	}
}
