


using DataUtility3.Transformers;

public class ReadResult<TReturn>
{

	public ReadResult(IEnumerable<ReadLineResult<TReturn>> results)
	{
		Valid = true;
		Errors = Enumerable.Empty<TransformationError>();
		Results = results;
	}

	public ReadResult(TransformationError error) : this(new List<TransformationError> { error })
	{ }
	public ReadResult(IEnumerable<TransformationError> errors)
	{
		Valid = false;
		Errors = errors;
		Results = Enumerable.Empty<ReadLineResult<TReturn>>();
	}
	public bool Valid { get; }

	public IEnumerable<TransformationError> Errors { get; }

	public IEnumerable<ReadLineResult<TReturn>> Results { get; }
}
