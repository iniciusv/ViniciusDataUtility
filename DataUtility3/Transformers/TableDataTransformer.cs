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

	public ReadResult<TModel> Transform(SimpleTableData tableData)
	{
		var results = new List<ReadLineResult<TModel>>();

		if (tableData == null || tableData.Headers == null || !tableData.Headers.Any())
		{
			return new ReadResult<TModel>(TransformationError.NoData);
		}

		int lineNumber = 1; // Cabeçalho é linha 1

		foreach (var row in tableData.Rows ?? Enumerable.Empty<List<string?>>())
		{
			lineNumber++;
			results.Add(TransformRow(row, tableData.Headers, lineNumber));
		}

		return new ReadResult<TModel>(results);
	}

	private ReadLineResult<TModel> TransformRow(List<string?> row, List<string> headers, int lineNumber)
	{
		var model = new TModel();
		var errors = new List<TransformationError>();
		var validationFailures = new List<ValidationFailure>();

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
				errors.Add(TransformationError.ConvertionFailed);
				validationFailures.Add(new ValidationFailure(header, $"Falha na conversão: {ex.Message}"));
			}
		}

		var validationResult = _validator.Validate(model);
		if (!validationResult.IsValid)
		{
			errors.Add(TransformationError.ConvertionFailed);
			validationFailures.AddRange(validationResult.Errors);
		}

		return new ReadLineResult<TModel>(
			lineNumber,
			errors.Any() ? null : model,
			errors,
			validationFailures);
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