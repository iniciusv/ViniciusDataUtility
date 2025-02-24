using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ClosedXML.Excel;
using DataUtility.Domain;

namespace DataUtility.Repository.Tables;

public static class ExcelProcessor
{
	/// <summary>
	/// Carrega e converte um arquivo Excel usando o caminho da pasta e o nome do arquivo.
	/// </summary>
	/// <param name="folderPath">Caminho da pasta onde o arquivo Excel está localizado.</param>
	/// <param name="fileName">Nome do arquivo Excel a ser carregado.</param>
	/// <returns>Objeto SimpleExcelData contendo os dados do arquivo Excel.</returns>
	public static SimpleTableData LoadAndConvertExcel(string folderPath, string fileName)
	{
		string fullPath = System.IO.Path.Combine(folderPath, fileName);
		var workbook = new XLWorkbook(fullPath);
		return ConvertToSimpleData(workbook);
	}

	/// <summary>
	/// Converte um XLWorkbook em um SimpleExcelData.
	/// </summary>
	/// <param name="workbook">O workbook do Excel a ser convertido.</param>
	/// <returns>Objeto SimpleExcelData com dados extraídos do workbook.</returns>
	private static SimpleTableData ConvertToSimpleData(XLWorkbook workbook)
	{
		var simpleData = new SimpleTableData();
		var sheet = workbook.Worksheet(1);
		var firstRowUsed = sheet.FirstRowUsed();
		var rowsUsed = sheet.RowsUsed();

		// Processa o cabeçalho
		simpleData.Headers = firstRowUsed.CellsUsed().Select(c => c.GetValue<string>()).ToList();

		// Processa as linhas de dados
		int numberOfHeaders = simpleData.Headers.Count;
		simpleData.Rows = rowsUsed.Skip(1).Select(row => {
			var rowList = new List<string>(new string[numberOfHeaders]);
			var cells = row.CellsUsed().ToList();
			for (int i = 0; i < cells.Count; i++)
			{
				int index = cells[i].Address.ColumnNumber - 1;
				if (index < numberOfHeaders)
				{
					rowList[index] = cells[i].GetValue<string>() ?? "";
				}
			}
			return rowList;
		}).ToList();

		return simpleData;
	}

	public static void CreateExcelFile(string folderPath, string fileName, SimpleTableData data)
	{
		IWorkbook workbook = new XSSFWorkbook();
		var sheet = workbook.CreateSheet("Sheet1");

		var headerRow = sheet.CreateRow(0);
		for (int columnIndex = 0; columnIndex < data.Headers.Count; columnIndex++)
		{
			headerRow.CreateCell(columnIndex).SetCellValue(data.Headers[columnIndex]);
		}

		for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
		{
			var excelRow = sheet.CreateRow(rowIndex + 1);
			var row = data.Rows[rowIndex];
			if (row.Count != data.Headers.Count)
			{
				throw new Exception("A quantidade de dados na linha não corresponde ao número de cabeçalhos.");
			}
			for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
			{
				excelRow.CreateCell(columnIndex).SetCellValue(row[columnIndex]);
			}
		}

		string fullPath = Path.Combine(folderPath, fileName);
		using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
		{
			workbook.Write(fileStream);
		}
	}
	public static void MergeSheetsIntoOne(string folderPath, string inputFileName, string outputFileName)
	{
		string inputFullPath = Path.Combine(folderPath, inputFileName);
		string outputFullPath = Path.Combine(folderPath, outputFileName);

		using (var inputWorkbook = new XLWorkbook(inputFullPath))
		{
			var outputWorkbook = new XLWorkbook();
			var outputSheet = outputWorkbook.Worksheets.Add("MergedSheet");
			HashSet<string> headers = new HashSet<string>();
			List<string> headersList = new List<string>();

			// Coletar todos os cabeçalhos das planilhas
			foreach (var sheet in inputWorkbook.Worksheets)
			{
				var row = sheet.FirstRowUsed();
				foreach (var cell in row.CellsUsed())
				{
					if (headers.Add(cell.GetString()))
					{
						headersList.Add(cell.GetString());
					}
				}
			}

			// Adicionar cabeçalhos à nova planilha
			var headerRow = outputSheet.Row(1);
			Dictionary<string, int> headerMap = new Dictionary<string, int>();
			for (int i = 0; i < headersList.Count; i++)
			{
				headerRow.Cell(i + 1).Value = headersList[i];
				headerMap[headersList[i]] = i + 1;
			}

			// Adicionar dados das planilhas à nova planilha
			int currentRow = 2;
			foreach (var sheet in inputWorkbook.Worksheets)
			{
				var sheetHeaders = sheet.FirstRowUsed().CellsUsed().Select(c => c.GetString()).ToList();
				var rows = sheet.RowsUsed().Skip(1); // Pular cabeçalho
				foreach (var row in rows)
				{
					var writingRow = outputSheet.Row(currentRow++);
					foreach (var cell in row.CellsUsed())
					{
						string header = sheetHeaders[cell.Address.ColumnNumber - 1];
						if (headerMap.ContainsKey(header))
						{
							writingRow.Cell(headerMap[header]).Value = cell.Value;
						}
					}
				}
			}

			// Salvar a nova planilha
			outputWorkbook.SaveAs(outputFullPath);
		}
	}
	public static void MergeMultipleExcelFilesIntoOneSheet(string folderPath, List<string> inputFileNames, string outputFileName)
	{
		string outputFullPath = Path.Combine(folderPath, outputFileName);
		var outputWorkbook = new XLWorkbook();
		var outputSheet = outputWorkbook.Worksheets.Add("MergedSheet");
		HashSet<string> headers = new HashSet<string>();
		List<string> headersList = new List<string>();

		// Processar cada arquivo
		foreach (var inputFileName in inputFileNames)
		{
			string inputFullPath = Path.Combine(folderPath, inputFileName);
			using (var inputWorkbook = new XLWorkbook(inputFullPath))
			{
				foreach (var sheet in inputWorkbook.Worksheets)
				{
					var row = sheet.FirstRowUsed();
					foreach (var cell in row.CellsUsed())
					{
						if (headers.Add(cell.GetString()))
						{
							headersList.Add(cell.GetString());
						}
					}
				}
			}
		}

		// Adicionar cabeçalhos à nova planilha
		var headerRow = outputSheet.Row(1);
		Dictionary<string, int> headerMap = new Dictionary<string, int>();
		for (int i = 0; i < headersList.Count; i++)
		{
			headerRow.Cell(i + 1).Value = headersList[i];
			headerMap[headersList[i]] = i + 1;
		}

		// Adicionar dados dos arquivos à nova planilha
		int currentRow = 2;
		foreach (var inputFileName in inputFileNames)
		{
			string inputFullPath = Path.Combine(folderPath, inputFileName);
			using (var inputWorkbook = new XLWorkbook(inputFullPath))
			{
				foreach (var sheet in inputWorkbook.Worksheets)
				{
					var sheetHeaders = sheet.FirstRowUsed().CellsUsed().Select(c => c.GetString()).ToList();
					var rows = sheet.RowsUsed().Skip(1); // Pular cabeçalho
					foreach (var row in rows)
					{
						var writingRow = outputSheet.Row(currentRow++);
						foreach (var cell in row.CellsUsed())
						{
							string header = sheetHeaders[cell.Address.ColumnNumber - 1];
							if (headerMap.ContainsKey(header))
							{
								writingRow.Cell(headerMap[header]).Value = cell.Value;
							}
						}
					}
				}
			}
		}

		// Salvar a nova planilha
		outputWorkbook.SaveAs(outputFullPath);
	}
}
