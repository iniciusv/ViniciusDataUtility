using ContractBid.Domain.Entities;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace ContractBid.Infra.Readers.Files.Models.Validatiors;

public class ResponseModelValidator : AbstractValidator<ResponseModel>
{
	public ResponseModelValidator()
	{
		RuleFor(x => x.SupplierMaterialCode)
			.NotEmpty().WithMessage("Código Material do Fornecedor é obrigatório.");

		RuleFor(x => x.MaterialNCM)
			.NotEmpty().WithMessage("Origem do Material é obrigatório.");

		RuleFor(x => x.IPI)
			.NotEmpty().WithMessage("IPI é obrigatório.")
			.Must(x => x == "SIM" || x == "NÃO").WithMessage("IPI deve ser 'SIM' ou 'NÃO'.");

		RuleFor(x => x.NetUnitPrice)
			.Must(BeAValidDecimal).WithMessage("NetUnitPrice deve ser um número válido e maior ou igual a zero.");

		RuleFor(x => x.MaterialOrigin)
			.NotEmpty().WithMessage("MaterialOrigin é obrigatório.")
			.Must(BeAValidMaterialOrigin).WithMessage("MaterialOrigin deve ser 'Nacional' ou 'Importado'.");
	}

	private bool BeAValidDecimal(string value)
	{
		return decimal.TryParse(value, out var result) && result >= 0;
	}


	private bool BeAValidMaterialOrigin(string value)
	{
		return value == "Nacional" || value == "Importado";
	}
}