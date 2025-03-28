using DataUtility.Domain;
using DataUtility3.Repository.Tables;
using FluentValidation;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility3.Transformers;

public class ModelBinder<TModel> where TModel : class, new()
{
	private readonly TableDataTransformer<TModel> Transformer;

	public ModelBinder(HeaderMapper<TModel> headerMapper,AbstractValidator<TModel> validator,ReaderConfig readerConfig)
	{
		if (!readerConfig.Converters.ContainsKey(typeof(decimal)))
			readerConfig.Converters.Add(typeof(decimal), s => decimal.Parse(s, CultureInfo.InvariantCulture));

		Transformer = new TableDataTransformer<TModel>(headerMapper, validator,	readerConfig);
	}

	public (List<TModel> Models, List<string> Errors) Bind(SimpleTableData tableData)
	{
		var errors = new List<string>();
		var models = new List<TModel>();

		try
		{
			if (tableData == null || tableData.Headers == null)
			{
				errors.Add("Dados da tabela inválidos");
				return (models, errors);
			}

			models = Transformer.Transform(tableData)
				.Where(model => model != null)
				.ToList();
		}
		catch (Exception ex)
		{
			errors.Add($"Erro durante o binding: {ex.Message}");
		}

		return (models, errors);
	}
}