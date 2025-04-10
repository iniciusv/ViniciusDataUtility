using DataUtility3.Transformers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility3.Repository.Tables;
public class ReaderConfig
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CSVReaderConfig"/> class.
	/// </summary>
	/// <param name="encoding">The encoding used to read/write the CSV file.</param>
	/// <param name="dateFormat">The format of the date fields in the CSV file.</param>
	/// <param name="decimalSeparator">The separator used for decimal numbers in the CSV file.</param>
	/// <param name="numberDecimalDigits">The number of decimal digits used for numeric values in the CSV file.</param>
	public ReaderConfig( Encoding encoding, string dateFormat, string decimalSeparator, int numberDecimalDigits = 2)
	{
		Encoding = encoding;
		DateFormat = dateFormat;
		NumberFormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = decimalSeparator, NumberDecimalDigits = numberDecimalDigits };
		Converters.Add(typeof(double), s => double.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(double?), s => double.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(decimal), s => decimal.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(decimal?), s => decimal.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(DateOnly), s => DateOnly.ParseExact(s, dateFormat));
		Converters.Add(typeof(DateOnly?), s => DateOnly.ParseExact(s, dateFormat));
		//Converters.Add(typeof(decimal), s => NumberParser.ParseDecimal(s));
		//Converters.Add(typeof(double), s => NumberParser.ParseDouble(s));
		Converters.Add(typeof(float), s => float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
	}
	public ReaderConfig()
	{
		
	}


	/// <summary>
	/// Gets the format of the date fields in the CSV file.
	/// </summary>
	public string DateFormat { get; }

	/// <summary>
	/// Gets the number format information used for numeric values in the CSV file.
	/// </summary>
	public NumberFormatInfo NumberFormatInfo { get; }

	/// <summary>
	/// Gets the encoding used to read/write the CSV file.
	/// </summary>
	public Encoding Encoding { get; }

	/// <summary>
	/// Gets a dictionary of converters used to parse string representations of data types into their respective types.
	/// </summary>
	public IDictionary<Type, Func<string, object>> Converters { get; } = new Dictionary<Type, Func<string, object>>();

	/// <summary>
	/// Attempts to get a converter for a specified data type.
	/// </summary>
	/// <param name="key">The data type for which to retrieve the converter.</param>
	/// <param name="value">When this method returns, contains the converter associated with the specified data type, if found; otherwise, null.</param>
	/// <returns>True if the converter was found; otherwise, false.</returns>
	public bool TryGetConverter(Type key, out Func<string, object> value) => Converters.TryGetValue(key, out value);
}
