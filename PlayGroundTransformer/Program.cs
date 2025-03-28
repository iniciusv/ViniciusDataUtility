using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using DataUtility3.Transformers;
using DataUtility.Repository.Tables;
using DataUtility3.Repository.Tables;
using PlayGroundTransformer;

class Program
{
	static void Main(string[] args)
	{
		const string filePath = @"C:\Config\User.csv";

		// 1. Configuração do leitor CSV
		var csvConfig = new CSVReaderConfig(
			fieldSeparator: ";",
			fieldMerger: "\"",
			lineSeparator: "\n",
			encoding: Encoding.UTF8,
			dateFormat: "dd/MM/yyyy",
			decimalSeparator: ",");

		// 2. Configuração do binder
		var readerConfig = new ReaderConfig(
			encoding: Encoding.UTF8,
			dateFormat: "dd/MM/yyyy",
			decimalSeparator: ",");

		var binder = new ModelBinder<User>(
			new UserMapper(),
			new UserValidator(),
			readerConfig);

		// 3. Leitura e processamento
		try
		{
			// Ler arquivo CSV
			var tableData = CSVProcessor.LoadAndConvertCSV(
				Path.GetDirectoryName(filePath),
				Path.GetFileName(filePath),
				csvConfig);

			// Fazer o binding
			var result = binder.Bind(tableData);
			var users = result.Models;
			var errors = result.Errors;

			// 4. Exibir resultados
			Console.WriteLine("=== USUÁRIOS IMPORTADOS ===");
			foreach (var user in users)
			{
				Console.WriteLine($"• {user.ClientCode}: {user.GUID} | NCM: {user.NCM} | Criado em: {user.Created:dd/MM/yyyy}");
			}

			if (errors.Any())
			{
				Console.WriteLine("\n=== ERROS ===");
				foreach (var error in errors)
				{
					Console.WriteLine($"- {error}");
				}
			}

			Console.WriteLine($"\nResumo: {users.Count} usuários importados, {errors.Count} erros");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro fatal: {ex.Message}");
		}
	}
}