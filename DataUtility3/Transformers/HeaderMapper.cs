using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace DataUtility3.Transformers
{
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


		/// <summary>
		/// Adiciona um mapeamento entre um cabeçalho e uma propriedade do modelo
		/// </summary>
		/// <param name="headerName">Nome do cabeçalho no arquivo</param>
		/// <param name="propertyExpression">Expressão lambda para acessar a propriedade</param>
		protected void Map(string headerName, Expression<Func<TModel, object>> propertyExpression)
		{
			if (string.IsNullOrWhiteSpace(headerName))
				throw new ArgumentException("Nome do cabeçalho não pode ser vazio", nameof(headerName));

			Mappings[headerName] = propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression));

			// Pré-cache da PropertyInfo
			if (!PropertyCache.ContainsKey(propertyExpression))
			{
				PropertyCache[propertyExpression] = GetPropertyInfo(propertyExpression);
			}
		}


		/// <summary>
		/// Tenta obter a expressão da propriedade associada a um cabeçalho
		/// </summary>
		/// <param name="headerName">Nome do cabeçalho</param>
		/// <param name="propertyExpression">Expressão da propriedade (saída)</param>
		/// <returns>True se o cabeçalho foi encontrado</returns>
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

		private static object ConvertValue(Type targetType, string value)
		{
			if (targetType == typeof(string)) return value;
			if (targetType == typeof(bool)) return value.Equals("SIM", StringComparison.OrdinalIgnoreCase);
			if (targetType.IsEnum) return Enum.Parse(targetType, value, true);

			var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
			return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
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
    

		/// <summary>
		/// Obtém todos os mapeamentos configurados
		/// </summary>
		public IEnumerable<KeyValuePair<string, Expression<Func<TModel, object>>>> GetMappings()
		{
			return Mappings;
		}

		/// <summary>
		/// Verifica se um cabeçalho específico está mapeado
		/// </summary>
		public bool ContainsHeader(string headerName)
		{
			return Mappings.ContainsKey(headerName);
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
	}
}