using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace DataUtility3.Transformers;

/// <summary>
/// Mapeador de cabeçalhos para propriedades de um modelo
/// </summary>
/// <typeparam name="TModel">Tipo do modelo que será mapeado</typeparam>
public abstract class HeaderMapper<TModel> where TModel : class
{
	private readonly Dictionary<string, Expression<Func<TModel, object>>> _mappings;
	private readonly Dictionary<Expression<Func<TModel, object>>, object> _staticValues;
	private readonly Dictionary<Expression<Func<TModel, object>>, Func<string, object>> _referenceResolvers = new();

	private readonly Dictionary<string, Func<string, object>> _headerReferenceResolvers = new();
	private readonly string _mapperName;

	protected HeaderMapper(string mapperName)
	{
		_mapperName = mapperName;
		_mappings = new Dictionary<string, Expression<Func<TModel, object>>>(StringComparer.OrdinalIgnoreCase);
		_staticValues = new Dictionary<Expression<Func<TModel, object>>, object>();
	}


	/// <summary>
	/// Adiciona um mapeamento entre um cabeçalho e uma propriedade do modelo
	/// </summary>
	/// <param name="headerName">Nome do cabeçalho no arquivo</param>
	/// <param name="propertyExpression">Expressão lambda para acessar a propriedade</param>
	protected void Map(string headerName, Expression<Func<TModel, object>> propertyExpression)
	{
		if (string.IsNullOrWhiteSpace(headerName))
		{
			throw new ArgumentException("Nome do cabeçalho não pode ser vazio", nameof(headerName));
		}

		_mappings[headerName] = propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression));
	}

	/// <summary>
	/// Tenta obter a expressão da propriedade associada a um cabeçalho
	/// </summary>
	/// <param name="headerName">Nome do cabeçalho</param>
	/// <param name="propertyExpression">Expressão da propriedade (saída)</param>
	/// <returns>True se o cabeçalho foi encontrado</returns>
	public bool TryGetProperty(string headerName, out Expression<Func<TModel, object>> propertyExpression)
	{
		return _mappings.TryGetValue(headerName, out propertyExpression);
	}

	/// <summary>
	/// Obtém todos os mapeamentos configurados
	/// </summary>
	public IEnumerable<KeyValuePair<string, Expression<Func<TModel, object>>>> GetMappings()
	{
		return _mappings;
	}

	/// <summary>
	/// Verifica se um cabeçalho específico está mapeado
	/// </summary>
	public bool ContainsHeader(string headerName)
	{
		return _mappings.ContainsKey(headerName);
	}

	/// <summary>
	/// Nome do mapeador (para identificação)
	/// </summary>
	public string MapperName => _mapperName;

	/// <summary>
	/// Define um valor estático para uma propriedade
	/// </summary>
	protected void SetStaticValue(Expression<Func<TModel, object>> propertyExpression, object value)
	{
		_staticValues[propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression))] = value;
	}

	/// <summary>
	/// Aplica valores estáticos ao modelo
	/// </summary>
	public void ApplyStaticValues(TModel model)
	{
		foreach (var staticValue in _staticValues)
		{
			var property = (staticValue.Key.Body as MemberExpression)?.Member as System.Reflection.PropertyInfo;
			if (property != null && property.CanWrite)
			{
				property.SetValue(model, staticValue.Value);
			}
		}
	}

	/// <summary>
	/// Define um resolvedor de referência para uma propriedade
	/// </summary>
	protected void SetReferenceResolver<TReference>(Expression<Func<TModel, object>> propertyExpression, Func<string, IEnumerable<TReference>> referenceSource, Func<string, TReference, bool> matchPredicate)
	{
		_referenceResolvers[propertyExpression] = referenceValue =>
		{
			var references = referenceSource(referenceValue);
			return references.FirstOrDefault(r => matchPredicate(referenceValue, r));
		};
	}
	/// <summary>
	/// Define um resolvedor de referência para um cabeçalho específico
	/// </summary>
	protected void MapReference<TObject>(
		string headerName,
		Func<string, IEnumerable<TObject>> referenceSource,
		Func<string, TObject, bool> matchPredicate)
	{
		_headerReferenceResolvers[headerName] = referenceValue =>
		{
			var references = referenceSource(referenceValue);
			return references.FirstOrDefault(r => matchPredicate(referenceValue, r));
		};
	}
	public bool TryResolveReference(string headerName, string referenceValue, out object resolvedObject)
	{
		if (_headerReferenceResolvers.TryGetValue(headerName, out var resolver))
		{
			resolvedObject = resolver(referenceValue);
			return resolvedObject != null;
		}
		resolvedObject = null;
		return false;
	}
	public virtual TModel CreateInstance()
	{
		return Activator.CreateInstance<TModel>();
	}
	public virtual void ApplySpecialMappings(TModel model, string header, string value)
	{
		// Pode ser sobrescrito por mapeadores específicos
	}
	/// <summary>
	/// Mapeia uma linha de dados para o modelo
	/// </summary>
	public TModel MapRow(Dictionary<string, string> rowData)
	{
		var model = CreateInstance();

		foreach (var kvp in rowData)
		{
			try
			{
				if (_headerReferenceResolvers.TryGetValue(kvp.Key, out var resolver))
				{
					// Resolve referência
					var reference = resolver(kvp.Value);
					if (reference != null)
					{
						var property = _mappings
							.FirstOrDefault(m => m.Key.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
							.Value;

						if (property != null)
						{
							var propertyInfo = GetPropertyInfo(property);
							propertyInfo.SetValue(model, reference);
						}
					}
				}
				else if (_mappings.TryGetValue(kvp.Key, out var propertyExpression))
				{
					// Mapeamento normal
					var propertyInfo = GetPropertyInfo(propertyExpression);
					SetPropertyValue(model, propertyInfo, kvp.Value);
				}

				ApplySpecialMappings(model, kvp.Key, kvp.Value);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					$"Erro ao mapear o campo '{kvp.Key}' com valor '{kvp.Value}'. {ex.Message}", ex);
			}
		}

		ApplyStaticValues(model);
		return model;
	}

	/// <summary>
	/// Define o valor de uma propriedade com tratamento de conversão de tipos
	/// </summary>
	protected void SetPropertyValue(TModel model, PropertyInfo propertyInfo, string stringValue)
	{
		if (string.IsNullOrEmpty(stringValue))
		{
			if (propertyInfo.PropertyType.IsValueType &&
				Nullable.GetUnderlyingType(propertyInfo.PropertyType) == null)
			{
				throw new InvalidOperationException(
					$"Não é possível converter valor vazio para {propertyInfo.PropertyType.Name}");
			}
			return;
		}

		try
		{
			object value;

			// Tratamento especial para tipos comuns
			if (propertyInfo.PropertyType == typeof(string))
			{
				value = stringValue;
			}
			else if (propertyInfo.PropertyType == typeof(bool))
			{
				value = stringValue.Equals("SIM", StringComparison.OrdinalIgnoreCase);
			}
			else if (propertyInfo.PropertyType.IsEnum)
			{
				value = Enum.Parse(propertyInfo.PropertyType, stringValue, true);
			}
			else
			{
				// Conversão padrão para outros tipos
				var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
				value = Convert.ChangeType(stringValue, underlyingType, CultureInfo.InvariantCulture);
			}

			propertyInfo.SetValue(model, value);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				$"Falha ao converter valor '{stringValue}' para {propertyInfo.PropertyType.Name}", ex);
		}
	}
	/// <summary>
	/// Obtém informações da propriedade a partir de uma expressão lambda
	/// </summary>
	protected PropertyInfo GetPropertyInfo(Expression<Func<TModel, object>> propertyExpression)
	{
		var member = propertyExpression.Body as MemberExpression;
		if (member == null && propertyExpression.Body is UnaryExpression unary)
		{
			member = unary.Operand as MemberExpression;
		}

		if (member?.Member is PropertyInfo propertyInfo)
		{
			return propertyInfo;
		}

		throw new ArgumentException("A expressão deve referenciar uma propriedade", nameof(propertyExpression));
	}

}