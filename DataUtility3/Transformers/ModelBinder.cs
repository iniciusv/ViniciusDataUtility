using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataUtility3.Transformers
{
	public class ModelBinder<TModel> where TModel : class, new()
	{
		private readonly TableDataTransformer<TModel> _transformer;
		private readonly HeaderMapper<TModel> _headerMapper;

		public ModelBinder(
			HeaderMapper<TModel> headerMapper,
			AbstractValidator<TModel> validator,
			ReaderConfig readerConfig)
		{
			_headerMapper = headerMapper ?? throw new ArgumentNullException(nameof(headerMapper));

			// Configuração padrão para decimal se não existir
			readerConfig.Converters.TryAdd(typeof(decimal),
				s => decimal.Parse(s, CultureInfo.InvariantCulture));

			_transformer = new TableDataTransformer<TModel>(
				headerMapper,
				validator,
				readerConfig);
		}

		public (List<TModel> Models, List<ReadLineResult<TModel>> LineResults) Bind(SimpleTableData tableData)
		{
			if (tableData?.Headers == null)
				return (new List<TModel>(), new List<ReadLineResult<TModel>>());

			var readResult = _transformer.Transform(tableData);
			return ProcessResults(readResult.Results);
		}

		private (List<TModel> Models, List<ReadLineResult<TModel>> LineResults) ProcessResults(
			IEnumerable<ReadLineResult<TModel>> results)
		{
			var validModels = new List<TModel>();
			var lineResults = new List<ReadLineResult<TModel>>();

			foreach (var lineResult in results)
			{
				lineResults.Add(lineResult);

				if (lineResult.IsValid && lineResult.Entity != null)
				{
					_headerMapper.ApplyStaticValues(lineResult.Entity);
					validModels.Add(lineResult.Entity);
				}
			}

			return (validModels, lineResults);
		}
	}
}