namespace DataUtility.Domain;

public class SimpleTableData
{
	public string TableName { get; set; }
	public List<string> Headers { get; set; }
	public TableSchema? Schema { get; set; }
	public List<List<string?>>? Rows { get; set; }

	public SimpleTableData()
	{
		Headers = new List<string>();
		Rows = new List<List<string?>>();
	}
}
