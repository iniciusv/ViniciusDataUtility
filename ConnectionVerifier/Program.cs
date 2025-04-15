using System;
using Microsoft.Data.SqlClient;
using DataUtility.DataBase.DataBases;

class Program
{
	static void Main(string[] args)
	{
		const string targetConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=bid-poc;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

		Console.WriteLine("Testando conexão com o banco de dados...");
		Console.WriteLine($"Connection string: {targetConnStr}");
		Console.WriteLine();

		try
		{
			using (var connectionManager = new ConnectionManager(targetConnStr))
			{
				Console.WriteLine("Tentando abrir conexão...");
				var connection = connectionManager.GetConnection();

				Console.WriteLine("Conexão aberta com sucesso!");
				Console.WriteLine($"Estado da conexão: {connection.State}");
				Console.WriteLine($"Banco de dados: {connection.Database}");
				Console.WriteLine($"Servidor: {connection.DataSource}");

				// Testar uma consulta simples
				Console.WriteLine("\nTestando consulta simples...");
				using (var command = new SqlCommand("SELECT 1", connection))
				{
					var result = command.ExecuteScalar();
					Console.WriteLine($"Resultado da consulta: {result}");
				}

				Console.WriteLine("\nTudo funcionou corretamente!");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nErro ao conectar ao banco de dados:");
			Console.WriteLine($"Mensagem: {ex.Message}");

			if (ex.InnerException != null)
			{
				Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
			}

			Console.WriteLine("\nConnection string pode estar incorreta ou o servidor não está acessível.");
		}

		Console.WriteLine("\nPressione qualquer tecla para sair...");
		Console.ReadKey();
	}
}