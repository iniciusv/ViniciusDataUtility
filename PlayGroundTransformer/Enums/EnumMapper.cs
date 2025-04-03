namespace ContractBid.Domain.Enums;

public static class EnumMapper
{
	public static BrazilStates MapToBrazilStates(WorkBookBrazilStateReference workBookState)
	{
		string name = Enum.GetName(typeof(WorkBookBrazilStateReference), workBookState);

		if (Enum.TryParse(name, out BrazilStates brazilState))
		{
			return brazilState;
		}
		throw new ArgumentException($"Não foi possível mapear {workBookState} para BrazilStates.");
	}
}