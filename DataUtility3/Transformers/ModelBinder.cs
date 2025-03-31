using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using FluentValidation;
using System.Globalization;

namespace DataUtility3.Transformers;


public class ModelBinder<TModel> where TModel : class, new()
{
	private readonly TableDataTransformer<TModel> _transformer;
	private readonly HeaderMapper<TModel> _headerMapper;

	public ModelBinder(
		HeaderMapper<TModel> headerMapper,
		AbstractValidator<TModel> validator,
		ReaderConfig readerConfig)
	{
		_headerMapper = headerMapper;

		if (!readerConfig.Converters.ContainsKey(typeof(decimal)))
			readerConfig.Converters.Add(typeof(decimal),
				s => decimal.Parse(s, CultureInfo.InvariantCulture));

		_transformer = new TableDataTransformer<TModel>(
			headerMapper,
			validator,
			readerConfig);
	}

	public (List<TModel> Models, List<ReadLineResult<TModel>> LineResults) Bind(SimpleTableData tableData)
	{
		var lineResults = new List<ReadLineResult<TModel>>();
		var validModels = new List<TModel>();

		if (tableData == null || tableData.Headers == null)
		{
			return (validModels, lineResults);
		}

		var readResult = _transformer.Transform(tableData);

		foreach (var lineResult in readResult.Results)
		{
			lineResults.Add(lineResult);

			if (lineResult.IsValid && lineResult.Entity != null)
			{
				// Aplica os valores estáticos e referências antes de adicionar o modelo
				_headerMapper.ApplyStaticValues(lineResult.Entity);
				validModels.Add(lineResult.Entity);
			}
		}

		return (validModels, lineResults);
	}
}
