
using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace DataUtility.Repository.Tables;

public static class CSVProcessor
{
	/// <summary>
	/// Carrega e converte um arquivo CSV em um objeto SimpleTableData usando a configuração especificada.
	/// </summary>
	public static SimpleTableData LoadAndConvertCSV(string folderPath, string fileName, CSVReaderConfig config)
	{
		string fullPath = Path.Combine(folderPath, fileName);
		var tableData = new SimpleTableData();

		using (var parser = new TextFieldParser(fullPath, config.Encoding))
		{
			parser.TextFieldType = FieldType.Delimited;
			parser.SetDelimiters(config.FieldSeparator);
			parser.HasFieldsEnclosedInQuotes = true; // Suposição, ajustar conforme necessidade

			// Processar cabeçalho
			if (!parser.EndOfData)
			{
				string[] headerFields = parser.ReadFields();
				tableData.Headers = new List<string>(headerFields);
			}

			int columns = tableData.Headers?.Count ?? 0;
			tableData.Rows = new List<List<string?>>();

			// Processar linhas
			while (!parser.EndOfData)
			{
				string[] fields = parser.ReadFields();
				var row = new List<string?>(new string?[columns]);

				for (int i = 0; i < columns && i < fields.Length; i++)
				{
					row[i] = fields[i];
				}

				tableData.Rows.Add(row);
			}
		}

		tableData.TableName = Path.GetFileNameWithoutExtension(fileName);
		return tableData;
	}

	/// <summary>
	/// Escreve um arquivo CSV a partir de um objeto SimpleTableData usando a configuração especificada.
	/// </summary>
	public static void WriteCSV(SimpleTableData data, string folderPath, string fileName, CSVReaderConfig config)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		if (data.Headers == null || data.Headers.Count == 0)
			throw new ArgumentException("O cabeçalho não pode ser nulo ou vazio", nameof(data));

		Directory.CreateDirectory(folderPath);
		string fullPath = Path.Combine(folderPath, fileName);

		using (var writer = new StreamWriter(fullPath, false, config.Encoding))
		{
			// Escrever cabeçalho
			writer.WriteLine(string.Join(config.FieldMerger, data.Headers));

			// Escrever linhas de dados
			if (data.Rows != null)
			{
				foreach (var row in data.Rows)
				{
					if (row.Count != data.Headers.Count)
						throw new ArgumentException($"Número de colunas na linha ({row.Count}) não corresponde ao cabeçalho ({data.Headers.Count})");

					writer.WriteLine(string.Join(config.FieldMerger, row.Select(field => FormatCsvField(field, config))));
				}
			}
		}
	}

	/// <summary>
	/// Formata um campo individual seguindo as regras CSV e a configuração especificada.
	/// </summary>
	private static string FormatCsvField(string? field, CSVReaderConfig config)
	{
		if (string.IsNullOrEmpty(field)) return string.Empty;

		// Verificar se precisa de escaping
		if (field.Contains(config.FieldMerger) || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
		{
			return $"\"{field.Replace("\"", "\"\"")}\"";
		}
		return field;
	}
}
