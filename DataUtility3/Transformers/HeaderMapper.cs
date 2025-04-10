using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataUtility3.Transformers;

public abstract class HeaderMapper<TModel> where TModel : class
{
	private readonly ConcurrentDictionary<string, Expression<Func<TModel, object>>> Mappings;
	private readonly ConcurrentDictionary<Expression<Func<TModel, object>>, object> StaticValues;
	private readonly ConcurrentDictionary<string, Func<string, object>> HeaderReferenceResolvers;
	private readonly string MapperName;

	private static readonly ConcurrentDictionary<Expression<Func<TModel, object>>, PropertyInfo> PropertyCache = new();

	protected HeaderMapper(string mapperName)
	{
		MapperName = mapperName;
		Mappings = new ConcurrentDictionary<string, Expression<Func<TModel, object>>>(StringComparer.OrdinalIgnoreCase);
		StaticValues = new ConcurrentDictionary<Expression<Func<TModel, object>>, object>();
		HeaderReferenceResolvers = new ConcurrentDictionary<string, Func<string, object>>(StringComparer.OrdinalIgnoreCase);
	}

	protected void Map(string headerName, Expression<Func<TModel, object>> propertyExpression)
	{
		if (string.IsNullOrWhiteSpace(headerName))
			throw new ArgumentException("Nome do cabeçalho não pode ser vazio", nameof(headerName));

		Mappings[headerName] = propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression));

		if (!PropertyCache.ContainsKey(propertyExpression))
		{
			PropertyCache[propertyExpression] = GetPropertyInfo(propertyExpression);
		}
	}

	public bool TryGetProperty(string headerName, out Expression<Func<TModel, object>> propertyExpression)
	{
		return Mappings.TryGetValue(headerName, out propertyExpression);
	}

	protected void SetStaticValue(Expression<Func<TModel, object>> propertyExpression, object value)
	{
		StaticValues[propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression))] = value;
	}

	protected void SetDynamicValue(Expression<Func<TModel, object>> propertyExpression, Func<object> valueFactory)
	{
		StaticValues[propertyExpression] = valueFactory;
	}

	public void ApplyStaticValues(TModel model)
	{
		if (model == null) return;

		foreach (var (propertyExpression, value) in StaticValues)
		{
			try
			{
				if (!PropertyCache.TryGetValue(propertyExpression, out var propertyInfo))
				{
					propertyInfo = GetPropertyInfo(propertyExpression);
					PropertyCache[propertyExpression] = propertyInfo;
				}

				if (propertyInfo != null && propertyInfo.CanWrite)
				{
					// Verifica se o valor precisa ser recalculado (como Guid.NewGuid())
					var actualValue = value is Func<object> func ? func() : value;
					propertyInfo.SetValue(model, actualValue);
				}
			}
			catch (Exception ex)
			{
				// Logar o erro para depuração
				Debug.WriteLine($"Error setting static value for {propertyExpression}: {ex.Message}");
			}
		}
	}

	protected void MapReference<TObject>(
		string headerName,
		Func<string, IEnumerable<TObject>> referenceSource,
		Func<string, TObject, bool> matchPredicate)
	{
		HeaderReferenceResolvers[headerName] = referenceValue =>
			referenceSource(referenceValue).FirstOrDefault(r => matchPredicate(referenceValue, r));
	}

	public bool TryResolveReference(string headerName, string referenceValue, out object resolvedObject)
	{
		if (HeaderReferenceResolvers.TryGetValue(headerName, out var resolver))
		{
			resolvedObject = resolver(referenceValue);
			return resolvedObject != null;
		}
		resolvedObject = null;
		return false;
	}

	public virtual TModel CreateInstance() => Activator.CreateInstance<TModel>();

	public virtual void ApplySpecialMappings(TModel model, string header, string value) { }

	public TModel MapRow(Dictionary<string, string> rowData)
	{
		var model = CreateInstance();

		foreach (var (key, value) in rowData)
		{
			try
			{
				ProcessField(model, key, value);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Erro ao mapear o campo '{key}' com valor '{value}'. {ex.Message}", ex);
			}
		}

		ApplyStaticValues(model);
		return model;
	}

	private void ProcessField(TModel model, string header, string value)
	{
		if (HeaderReferenceResolvers.TryGetValue(header, out var resolver))
		{
			ProcessReferenceField(model, header, resolver(value));
		}
		else if (Mappings.TryGetValue(header, out var propertyExpression))
		{
			ProcessRegularField(model, propertyExpression, value);
		}

		ApplySpecialMappings(model, header, value);
	}

	private void ProcessReferenceField(TModel model, string header, object reference)
	{
		if (reference == null) return;

		if (Mappings.TryGetValue(header, out var propertyExpression) &&
			PropertyCache.TryGetValue(propertyExpression, out var propertyInfo))
		{
			propertyInfo.SetValue(model, reference);
		}
	}

	private void ProcessRegularField(TModel model, Expression<Func<TModel, object>> propertyExpression, string value)
	{
		if (!PropertyCache.TryGetValue(propertyExpression, out var propertyInfo)) return;

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

		var convertedValue = ConvertValue(propertyInfo.PropertyType, value);
		propertyInfo.SetValue(model, convertedValue);
	}


	protected static PropertyInfo GetPropertyInfo(Expression<Func<TModel, object>> propertyExpression)
	{
		var member = propertyExpression.Body switch
		{
			MemberExpression m => m,
			UnaryExpression { Operand: MemberExpression m } => m,
			_ => throw new ArgumentException("A expressão deve referenciar uma propriedade", nameof(propertyExpression))
		};

		return (PropertyInfo)member.Member;
	}

	public IEnumerable<KeyValuePair<string, Expression<Func<TModel, object>>>> GetMappings()
	{
		return Mappings;
	}


	private static object ConvertValue(Type targetType, string value)
	{
		if (targetType == typeof(string)) return value;
		if (targetType == typeof(bool)) return value.Equals("SIM", StringComparison.OrdinalIgnoreCase);
		if (targetType.IsEnum) return Enum.Parse(targetType, value, true);

		var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

		// Tratamento especial para números decimais
		if (underlyingType == typeof(decimal))
		{
			return ParseDecimal(value);
		}
		else if (underlyingType == typeof(double))
		{
			return ParseDouble(value);
		}

		return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
	}

	private static decimal ParseDecimal(string value)
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

	private static double ParseDouble(string value)
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