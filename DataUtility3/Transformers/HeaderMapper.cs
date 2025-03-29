using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DataUtility3.Transformers;

/// <summary>
/// Mapeador de cabeçalhos para propriedades de um modelo
/// </summary>
/// <typeparam name="TModel">Tipo do modelo que será mapeado</typeparam>
public abstract class HeaderMapper<TModel> where TModel : class
{
	private readonly Dictionary<string, Expression<Func<TModel, object>>> _mappings;
	private readonly Dictionary<Expression<Func<TModel, object>>, object> _staticValues;
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

}