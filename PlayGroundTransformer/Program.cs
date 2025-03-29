using DataUtility.Repository.Tables;
using DataUtility3.Repository.Tables;
using DataUtility3.Transformers;
using PlayGroundTransformer;
using System.Text;

class Program
{
	static void Main(string[] args)
	{
		const string filePath = @"C:\Config\User.csv";

		var csvConfig = new CSVReaderConfig(
			fieldSeparator: ";",
			fieldMerger: "\"",
			lineSeparator: "\n",
			encoding: Encoding.UTF8,
			dateFormat: "dd/MM/yyyy",
			decimalSeparator: ",");

		var readerConfig = new ReaderConfig(
			encoding: Encoding.UTF8,
			dateFormat: "dd/MM/yyyy",
			decimalSeparator: ",");

		var profiles = new Profile()
		{
			Name = "saudoso"
		};
		var profileList = new List<Profile>();
		profileList.Add(profiles);

		var binder = new ModelBinder<User>(
			new UserMapper("jhghj", profileList),
			new UserValidator(),
			readerConfig);

		try
		{
			var tableData = CSVProcessor.LoadAndConvertCSV(
				Path.GetDirectoryName(filePath),
				Path.GetFileName(filePath),
				csvConfig);

			var (users, lineResults) = binder.Bind(tableData);

			Console.WriteLine("=== USUÁRIOS VÁLIDOS ===");
			foreach (var user in users)
			{
				Console.WriteLine($"• {user.ClientCode}: {user.GUID}");
			}

			var errorResults = lineResults.Where(r => !r.IsValid).ToList();
			if (errorResults.Any())
			{
				Console.WriteLine("\n=== ERROS DETALHADOS ===");
				foreach (var error in errorResults)
				{
					Console.WriteLine($"Linha {error.LineNumber}:");
					foreach (var validationError in error.ValidationFailures)
					{
						Console.WriteLine($"- {validationError.PropertyName}: {validationError.ErrorMessage}");
					}
				}
			}

			Console.WriteLine($"\nResumo: {users.Count} válidos, {errorResults.Count} linhas com erro");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro fatal: {ex.Message}");
		}
	}
}