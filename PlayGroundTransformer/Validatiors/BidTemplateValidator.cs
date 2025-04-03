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

public class BidTemplateValidator : AbstractValidator<Response>
{
	public BidTemplateValidator()
	{
		RuleFor(x => x.Request.Material).NotNull().WithMessage("Material deve ser especificado");
		RuleFor(x => x.Date).LessThanOrEqualTo(DateTime.Today.AddYears(1))
			.WithMessage("Data inválida");
	}
}