namespace DataUtility3.Transformers;

/// <summary>
/// Define problems on CSV import.
/// </summary>
[Flags]
public enum TransformationError
{
	/// <summary>
	/// Could not find a constructor or way to instanciate the entity.
	/// </summary>
	Creation = 1,
	/// <summary>
	/// Csv does not have the required number of columns.
	/// </summary>
	MissingColumn = 2,
	/// <summary>
	/// CSV has no valid data.
	/// </summary>
	NoData = 4,
	/// <summary>
	/// A reference value cannot be found.
	/// </summary>
	ReferenceNotFound = 8,
	/// <summary>
	/// Indicates that a required field is empty.
	/// </summary>
	RequiredFieldEmpty = 16,
	/// <summary>
	/// Indicates that the conversion of the input to the type of the property in the entity was not possible
	/// </summary>
	ConvertionFailed = 32,
	/// <summary>
	/// Values separator from file are diferente from config.
	/// </summary>
	InvalidSeparator = 64,
	/// <summary>
	/// More than one occurrence was found for a value that should be unique.
	/// </summary>
	DuplicatedValue = 128
}
