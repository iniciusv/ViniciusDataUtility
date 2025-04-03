using ContractBid.Domain.Enums;


namespace ContractBid.Infra.Readers.Files.Models;

public class ResponseModel 
{
	public string? MaterialCode { get; set; }
	public string? MaterialDescription { get; set; }
	public string? EstimatedQuantity { get; set; }
	public string? SupplierMaterialCode { get; set; }
	public string? MaterialOrigin { get; set; }
	public string? MaterialNCM { get; set; }
	public string? SupplyMultiples { get; set; }
	public string? IPI { get; set; }
	public string? NetUnitPrice { get; set; }
	public string Reference { get; set; }
	public string ReferenceUnitPrice { get; set; }
	public IDictionary<BrazilStates, string?> StatePrices { get; set; }
	public string BidGuid { get; set; }
}