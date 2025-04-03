using ContractBid.Domain.Enums;

namespace ContractBid.Domain.Entities;

public class ScenarioResponse : BaseEntity
{
	public Request Request { get; set; }
	public Response Response { get; set; }
	public bool Responsed => Response != null;
	public ScenarioResponse(Request request)
	{
		Request = request;
	}
	public ScenarioResponse(Request request, Response response)
	{
		Request = request;
		Response = response;
	}
	public ScenarioResponse Clone() => new ScenarioResponse(Request, Response);
}
