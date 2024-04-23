using FluentValidation;
using PeopleManagement.Application.Commands.CreateCompany;
using PeopleManagement.Application.Extension;
using PeopleManagement.Application.Utils;
using PeopleManagement.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace PeopleManagement.Application.Validations
{
    public class CompanyValidator : AbstractValidator<CreateCompanyCommand>
    {
        public CompanyValidator() 
        { 
            RuleFor(x => x.CorporateName).NotEmpty().WithMessage(x => DomainErrors.FieldInvalid(nameof(x.CorporateName), x.CorporateName!.ToString())).NotNull().MaximumLength(150)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.CorporateName), x.CorporateName!.ToString())); 
            RuleFor(x => x.FantasyName).NotEmpty().NotNull().MaximumLength(100)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.FantasyName), x.FantasyName!.ToString())); 
            RuleFor(x => x.Description).NotNull().MaximumLength(500)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Description), x.Description!.ToString()));
            RuleFor(x => x.Cnpj)
                .NotEmpty()
                .NotNull()
                .MaximumLength(18)
                .Must((obj, prop, context) => ValidateCNPJ.ValidaCNPJ(prop))
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Cnpj), x.Cnpj!));
            RuleFor(x => x.Email)
                .NotNull()
                .MaximumLength(100)
                .Matches(@"/^[a-z0-9.]+@[a-z0-9]+\\.[a-z]+\\.([a-z]+)?$/i")
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Phone), x.Phone!.ToString())); ;
            RuleFor(x => x.Phone)
                .NotNull()
                .MaximumLength(20)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Phone), x.Phone!.ToString()));
            RuleFor(x => x.Street).NotEmpty().NotNull().MaximumLength(100)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Street), x.Street!.ToString()));
            RuleFor(x => x.City).NotEmpty().NotNull().MaximumLength(50)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.City), x.City!.ToString()));
            RuleFor(x => x.State).NotEmpty().NotNull().MaximumLength(50)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.State), x.State!.ToString())); 
            RuleFor(x => x.Country).NotEmpty().NotNull().MaximumLength(50)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.Country), x.Country!.ToString())); 
            RuleFor(x => x.ZipCode).NotEmpty().NotNull().MaximumLength(16)
                .WithMessage(x => DomainErrors.FieldInvalid(nameof(x.ZipCode), x.ZipCode!.ToString())); 
        }
    }
}
