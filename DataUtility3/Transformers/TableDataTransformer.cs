using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using FluentValidation;

using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility3.Transformers;

public class TableDataTransformer<TModel> where TModel : class, new()
{
	private readonly HeaderMapper<TModel> _headerMapper;
	private readonly AbstractValidator<TModel> _validator;
	private readonly ReaderConfig _readerConfig;

	public TableDataTransformer(
		HeaderMapper<TModel> headerMapper,
		AbstractValidator<TModel> validator,
		ReaderConfig readerConfig)
	{
		_headerMapper = headerMapper;
		_validator = validator;
		_readerConfig = readerConfig;
	}

	public IEnumerable<TModel> Transform(SimpleTableData tableData)
	{
		if (tableData == null || tableData.Headers == null || !tableData.Headers.Any())
		{
			yield break; // Retorna uma coleção vazia
		}

		var lineNumber = 1; // Começa em 1 porque o cabeçalho é linha 0

		foreach (var row in tableData.Rows ?? Enumerable.Empty<List<string?>>())
		{
			lineNumber++;
			var model = TransformRow(row, tableData.Headers, lineNumber);

			if (model != null)
			{
				yield return model;
			}
		}
	}

	private TModel? TransformRow(List<string?> row, List<string> headers, int lineNumber)
	{
		var model = new TModel();
		bool hasErrors = false;

		for (int i = 0; i < Math.Min(headers.Count, row.Count); i++)
		{
			var header = headers[i];
			var value = row[i] ?? string.Empty;

			try
			{
				if (_headerMapper.TryGetProperty(header, out var propertySelector))
				{
					var propertyInfo = GetPropertyInfo(model, propertySelector);
					SetPropertyValue(model, propertyInfo, value);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erro na linha {lineNumber}, coluna '{header}': {ex.Message}");
				hasErrors = true;
			}
		}

		// Executa a validação FluentValidation
		var validationResult = _validator.Validate(model);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
			{
				Console.WriteLine($"Erro de validação na linha {lineNumber}: {error.ErrorMessage}");
			}
			hasErrors = true;
		}

		return hasErrors ? null : model;
	}

	private PropertyInfo GetPropertyInfo(TModel model, Expression<Func<TModel, object>> propertySelector)
	{
		var member = propertySelector.Body as MemberExpression;
		if (member == null && propertySelector.Body is UnaryExpression unary)
		{
			member = unary.Operand as MemberExpression;
		}

		return (PropertyInfo)member.Member;
	}

	private void SetPropertyValue(TModel model, PropertyInfo propertyInfo, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			if (propertyInfo.PropertyType.IsValueType && Nullable.GetUnderlyingType(propertyInfo.PropertyType) == null)
			{
				throw new InvalidOperationException($"Não é possível converter valor vazio para {propertyInfo.PropertyType.Name}");
			}
			return;
		}

		try
		{
			if (_readerConfig.TryGetConverter(propertyInfo.PropertyType, out var converter))
			{
				var convertedValue = converter(value);
				propertyInfo.SetValue(model, convertedValue);
			}
			else
			{
				// Conversão padrão para tipos não registrados
				var convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);
				propertyInfo.SetValue(model, convertedValue);
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Falha ao converter valor '{value}' para {propertyInfo.PropertyType.Name}", ex);
		}
	}
}