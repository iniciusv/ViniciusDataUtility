using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace DataUtility3.Transformers;

public class ReadLineResult<TModel>
{
	public ReadLineResult(
		int lineNumber,
		TModel? entity,
		IEnumerable<TransformationError> errors,
		IEnumerable<ValidationFailure> validationFailures)
	{
		LineNumber = lineNumber;
		Entity = entity;
		Errors = errors;
		ValidationFailures = validationFailures;
	}

	public int LineNumber { get; }
	public TModel? Entity { get; }
	public IEnumerable<TransformationError> Errors { get; }
	public IEnumerable<ValidationFailure> ValidationFailures { get; }
	public bool IsValid => !Errors.Any() && !ValidationFailures.Any();
}