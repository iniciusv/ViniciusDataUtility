using Microsoft.Data.SqlClient;

namespace DataUtility;

public class ConnectionManager : IDisposable
{
	private readonly SqlConnectionStringBuilder _connectionStringBuilder;
	private SqlConnection _connection;

	public ConnectionManager(string baseConnectionString)
	{
		_connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString);
	}

	// Novo construtor que aceita tanto a string base quanto o nome do banco de dados
	public ConnectionManager(string baseConnectionString, string databaseName) : this(baseConnectionString)
	{
		if (!string.IsNullOrEmpty(databaseName))
		{
			_connectionStringBuilder.InitialCatalog = databaseName;
		}
	}

	public SqlConnection GetConnection(string databaseName = null)
	{
		if (_connection != null)
		{
			_connection.Close();
		}

		// Se um nome de banco de dados é fornecido e não está já na string de conexão, atualiza-o
		if (!string.IsNullOrEmpty(databaseName) && _connectionStringBuilder.InitialCatalog != databaseName)
		{
			_connectionStringBuilder.InitialCatalog = databaseName;
		}

		// Cria uma nova conexão com a string de conexão atualizada
		var connection = _connectionStringBuilder.ToString();

		_connection = new SqlConnection(_connectionStringBuilder.ToString());
		_connection.Open();
		return _connection;
	}

	public void CloseConnection()
	{
		if (_connection != null)
		{
			_connection.Close();
			_connection.Dispose();
			_connection = null;
		}
	}

	public void Dispose()
	{
		CloseConnection();
	}
}
