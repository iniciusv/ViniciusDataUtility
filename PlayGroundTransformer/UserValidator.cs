using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGroundTransformer;

public class UserValidator : AbstractValidator<User>
{
	public UserValidator()
	{
		RuleFor(x => x.GUID)
			.NotEmpty().WithMessage("GUID é obrigatório");

		RuleFor(x => x.Created)
			.NotEmpty().WithMessage("Data de criação é obrigatória")
			.LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

		RuleFor(x => x.ClientCode)
			.NotEmpty().WithMessage("Código do cliente é obrigatório")
			.MaximumLength(20).WithMessage("Código muito longo");

		RuleFor(x => x.Description)
			.MaximumLength(100).WithMessage("Descrição muito longa");

		RuleFor(x => x.NCM)
			.GreaterThan(0).WithMessage("NCM deve ser positivo");
	}


}