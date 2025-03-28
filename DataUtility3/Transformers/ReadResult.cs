


using DataUtility3.Transformers;

/// <summary>
/// Represents the result of reading a CSV file, including a collection of line-by-line read results and overall validation status.
/// </summary>
/// <typeparam name="TReturn">The type of the parsed entity.</typeparam>
public class ReadResult<TReturn>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ReadResult{TReturn}"/> class with a collection of line-by-line read results.
	/// </summary>
	/// <param name="results">The collection of line-by-line read results.</param>
	public ReadResult(IEnumerable<ReadLineResult<TReturn>> results)
	{
		Valid = true;
		Errors = Enumerable.Empty<TransformationError>();
		Results = results;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ReadResult{TReturn}"/> class with a single error.
	/// </summary>
	/// <param name="error">The single error encountered during CSV reading.</param>
	public ReadResult(TransformationError error) : this(new List<TransformationError> { error })
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="ReadResult{TReturn}"/> class with a collection of errors.
	/// </summary>
	/// <param name="errors">The collection of errors encountered during CSV reading.</param>
	public ReadResult(IEnumerable<TransformationError> errors)
	{
		Valid = false;
		Errors = errors;
		Results = Enumerable.Empty<ReadLineResult<TReturn>>();
	}

	/// <summary>
	/// Gets a value indicating whether the CSV reading operation was successful overall.
	/// </summary>
	public bool Valid { get; }

	/// <summary>
	/// Gets the collection of errors encountered during CSV reading.
	/// </summary>
	public IEnumerable<TransformationError> Errors { get; }

	/// <summary>
	/// Gets the collection of line-by-line read results.
	/// </summary>
	public IEnumerable<ReadLineResult<TReturn>> Results { get; }
}
