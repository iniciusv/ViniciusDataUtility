using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataUtility3.Transformers;

public static class NumberParser
{
	public static decimal ParseDecimal(string value)
	{
		// Remove espaços e caracteres não numéricos (exceto ponto e vírgula)
		value = Regex.Replace(value.Trim(), @"[^\d.,-]", "");

		// Verifica se o último separador é o decimal (para casos como 1.234,56)
		if (value.LastIndexOf(',') > value.LastIndexOf('.'))
		{
			// Formato europeu (vírgula como decimal)
			return decimal.Parse(value, CultureInfo.GetCultureInfo("pt-BR"));
		}

		// Tenta parse com cultura invariante (usa ponto como decimal)
		if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}

		// Se falhar, tenta com cultura brasileira
		return decimal.Parse(value, CultureInfo.GetCultureInfo("pt-BR"));
	}

	public static double ParseDouble(string value)
	{
		// Lógica similar ao ParseDecimal
		value = Regex.Replace(value.Trim(), @"[^\d.,-]", "");

		if (value.LastIndexOf(',') > value.LastIndexOf('.'))
		{
			return double.Parse(value, CultureInfo.GetCultureInfo("pt-BR"));
		}

		if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}

		return double.Parse(value, CultureInfo.GetCultureInfo("pt-BR"));
	}
}
