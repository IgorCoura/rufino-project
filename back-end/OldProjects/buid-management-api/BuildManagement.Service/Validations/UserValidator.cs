using BuildManagement.Domain.Models.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations
{
    public class CreateUserValidator : AbstractValidator<CreateUserModel>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.UserName)
                .MinimumLength(3)
                .MaximumLength(50)
                .NotEmpty();

            RuleFor(x => x.Password)
                .MinimumLength(3)
                .MaximumLength(200)
                .NotEmpty();

            RuleFor(x => x.Password)
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$_!%*?&-])[A-Za-z\d@$!_%*?&-]{8,}$")
                .WithMessage("Senha deve conter o mínimo de oito caracteres, pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial.");
        }
    }
}
