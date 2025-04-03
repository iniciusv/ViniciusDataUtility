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

public class MaterialValidator : AbstractValidator<Material>
{
	public MaterialValidator()
	{

	}
}