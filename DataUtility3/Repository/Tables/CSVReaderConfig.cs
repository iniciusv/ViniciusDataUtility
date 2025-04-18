﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility3.Repository.Tables;
public class CSVReaderConfig
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CSVReaderConfig"/> class.
	/// </summary>
	/// <param name="fieldSeparator">The separator used between fields in the CSV file.</param>
	/// <param name="fieldMerger">The string used to merge fields when writing to CSV.</param>
	/// <param name="lineSeparator">The string used to separate lines in the CSV file.</param>
	/// <param name="encoding">The encoding used to read/write the CSV file.</param>
	/// <param name="dateFormat">The format of the date fields in the CSV file.</param>
	/// <param name="decimalSeparator">The separator used for decimal numbers in the CSV file.</param>
	/// <param name="numberDecimalDigits">The number of decimal digits used for numeric values in the CSV file.</param>
	public CSVReaderConfig(string fieldSeparator, string fieldMerger, string lineSeparator, Encoding encoding, string dateFormat, string decimalSeparator, int numberDecimalDigits = 2)
	{
		FieldSeparator = fieldSeparator;
		FieldMerger = fieldMerger;
		LineSeparator = lineSeparator;
		Encoding = encoding;
		DateFormat = dateFormat;
		NumberFormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = decimalSeparator, NumberDecimalDigits = numberDecimalDigits };
		Converters.Add(typeof(double), s => double.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(double?), s => double.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(decimal), s => decimal.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(decimal?), s => decimal.Parse(s, NumberFormatInfo));
		Converters.Add(typeof(DateOnly), s => DateOnly.ParseExact(s, dateFormat));
		Converters.Add(typeof(DateOnly?), s => DateOnly.ParseExact(s, dateFormat));
	}

	/// <summary>
	/// Gets the separator used between fields in the CSV file.
	/// </summary>
	public string FieldSeparator { get; }

	/// <summary>
	/// Gets the string used to merge fields when writing to CSV.
	/// </summary>
	public string FieldMerger { get; }

	/// <summary>
	/// Gets the string used to separate lines in the CSV file.
	/// </summary>
	public string LineSeparator { get; }

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
	IDictionary<Type, Func<string, object>> Converters { get; } = new Dictionary<Type, Func<string, object>>();

	/// <summary>
	/// Attempts to get a converter for a specified data type.
	/// </summary>
	/// <param name="key">The data type for which to retrieve the converter.</param>
	/// <param name="value">When this method returns, contains the converter associated with the specified data type, if found; otherwise, null.</param>
	/// <returns>True if the converter was found; otherwise, false.</returns>
	public bool TryGetConverter(Type key, out Func<string, object> value) => Converters.TryGetValue(key, out value);
}
