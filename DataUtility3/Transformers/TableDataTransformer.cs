using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using FluentValidation;
using FluentValidation.Results;

namespace DataUtility3.Transformers;

public class TableDataTransformer<TModel> where TModel : class, new()
{
	private readonly HeaderMapper<TModel> _headerMapper;
	private readonly AbstractValidator<TModel> _validator;
	private readonly ReaderConfig _readerConfig;

	// Cache para PropertyInfo
	private static readonly ConcurrentDictionary<Expression<Func<TModel, object>>, PropertyInfo> _propertyCache = new();

	public TableDataTransformer(
		HeaderMapper<TModel> headerMapper,
		AbstractValidator<TModel> validator,
		ReaderConfig readerConfig)
	{
		_headerMapper = headerMapper ?? throw new ArgumentNullException(nameof(headerMapper));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_readerConfig = readerConfig ?? throw new ArgumentNullException(nameof(readerConfig));
	}

	public ReadResult<TModel> Transform(SimpleTableData tableData)
	{
		if (tableData?.Headers == null || !tableData.Headers.Any())
			return new ReadResult<TModel>(TransformationError.NoData);

		var results = new List<ReadLineResult<TModel>>();
		var lineNumber = 1; // Cabeçalho é linha 1

		foreach (var row in tableData.Rows ?? Enumerable.Empty<List<string?>>())
		{
			lineNumber++;
			results.Add(TransformRow(row, tableData.Headers, lineNumber));
		}

		return new ReadResult<TModel>(results);
	}

	private ReadLineResult<TModel> TransformRow(List<string?> row, List<string> headers, int lineNumber)
	{
		var model = _headerMapper.CreateInstance();
		var errors = new List<TransformationError>();
		var validationFailures = new List<ValidationFailure>();

		ProcessRowFields(model, row, headers, errors, validationFailures);

		if (!errors.Any())
		{
			ValidateModel(model, errors, validationFailures);
		}

		return new ReadLineResult<TModel>(
			lineNumber,
			errors.Any() ? null : model,
			errors,
			validationFailures);
	}

	private void ProcessRowFields(TModel model, List<string?> row, List<string> headers,
		List<TransformationError> errors, List<ValidationFailure> validationFailures)
	{
		for (int i = 0; i < Math.Min(headers.Count, row.Count); i++)
		{
			var header = headers[i];
			var value = row[i] ?? string.Empty;

			try
			{
				ProcessField(model, header, value);
			}
			catch (Exception ex)
			{
				errors.Add(TransformationError.ConvertionFailed);
				validationFailures.Add(new ValidationFailure(header, $"Falha na conversão: {ex.Message}"));
			}
		}
	}

	private void ProcessField(TModel model, string header, string value)
	{
		if (_headerMapper.TryResolveReference(header, value, out var reference))
		{
			SetReferenceValue(model, header, reference);
		}
		else if (_headerMapper.TryGetProperty(header, out var propertySelector))
		{
			SetPropertyValue(model, propertySelector, value);
		}

		_headerMapper.ApplySpecialMappings(model, header, value);
	}

	private void SetReferenceValue(TModel model, string header, object reference)
	{
		if (reference == null) return;

		var property = _headerMapper.GetMappings()
			.FirstOrDefault(m => m.Key.Equals(header, StringComparison.OrdinalIgnoreCase))
			.Value;

		if (property != null)
		{
			var propertyInfo = GetPropertyInfo(property);
			propertyInfo?.SetValue(model, reference);
		}
	}

	private void SetPropertyValue(TModel model, Expression<Func<TModel, object>> propertySelector, string value)
	{
		var propertyInfo = GetPropertyInfo(propertySelector);
		if (propertyInfo == null) return;

		if (string.IsNullOrEmpty(value))
		{
			if (propertyInfo.PropertyType.IsValueType &&
				Nullable.GetUnderlyingType(propertyInfo.PropertyType) == null)
			{
				throw new InvalidOperationException(
					$"Não é possível converter valor vazio para {propertyInfo.PropertyType.Name}");
			}
			return;
		}

		object convertedValue;

		// Tratamento especial para tipos numéricos
		if (propertyInfo.PropertyType == typeof(decimal) ||
			Nullable.GetUnderlyingType(propertyInfo.PropertyType) == typeof(decimal))
		{
			convertedValue = NumberParser.ParseDecimal(value);
		}
		else if (propertyInfo.PropertyType == typeof(double) ||
				 Nullable.GetUnderlyingType(propertyInfo.PropertyType) == typeof(double))
		{
			convertedValue = NumberParser.ParseDouble(value);
		}
		else
		{
			convertedValue = _readerConfig.TryGetConverter(propertyInfo.PropertyType, out var converter)
				? converter(value)
				: Convert.ChangeType(value, propertyInfo.PropertyType);
		}

		propertyInfo.SetValue(model, convertedValue);
	}

	private void ValidateModel(TModel model, List<TransformationError> errors, List<ValidationFailure> validationFailures)
	{
		var validationResult = _validator.Validate(model);
		if (!validationResult.IsValid)
		{
			errors.Add(TransformationError.ConvertionFailed);
			validationFailures.AddRange(validationResult.Errors);
		}
	}

	private PropertyInfo GetPropertyInfo(Expression<Func<TModel, object>> propertySelector)
	{
		return _propertyCache.GetOrAdd(propertySelector, expr =>
		{
			var member = expr.Body switch
			{
				MemberExpression m => m,
				UnaryExpression { Operand: MemberExpression m } => m,
				_ => throw new InvalidOperationException("Expressão inválida para propriedade")
			};
			return (PropertyInfo)member.Member;
		});
	}
}