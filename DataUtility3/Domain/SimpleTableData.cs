namespace DataUtility.Domain;

public class SimpleTableData
{
	public string TableName { get; set; }
	public List<string> Headers { get; set; }
	public TableSchema? Schema { get; set; }
	public List<List<string?>>? Rows { get; set; }

	// Construtor que inicializa Headers e Rows
	public SimpleTableData()
	{
		Headers = new List<string>(); // Assegura que Headers é sempre inicializado
		Rows = new List<List<string?>>(); // Assegura que Rows é sempre inicializado
	}
}
