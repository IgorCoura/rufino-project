using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;
using System;
using System.Collections.Generic;
namespace PeopleManagement.Application.Commands.CompanyCommands.EditCompany
{
    public class EditCompanyCommandHandler(ICompanyRepository companyRepository) : IRequestHandler<EditCompanyCommand, EditCompanyResponse>
    {
        private readonly ICompanyRepository _companyRepository = companyRepository;

        public async Task<EditCompanyResponse> Handle(EditCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _companyRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), request.Id.ToString()));

            company.Edit(
                request.CorporateName,
                request.FantasyName,
                request.Cnpj,
                Contact.Create(
                    request.Email, 
                    request.Phone),
                Address.Create(
                    request.ZipCode, 
                    request.Street, 
                    request.Number, 
                    request.Complement, 
                    request.Neighborhood, 
                    request.City, 
                    request.State, 
                    request.Country)
                );

            await _companyRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                
            return company.Id;
        }
    }

    public class EditCompanyIdentifiedCommandHandler : IdentifiedCommandHandler<EditCompanyCommand, EditCompanyResponse>
    {
        public EditCompanyIdentifiedCommandHandler(IMediator mediator,
            ILogger<IdentifiedCommandHandler<EditCompanyCommand, EditCompanyResponse>> logger, IRequestManager requestManager)
            : base(mediator, logger, requestManager)
        {
        }

        protected override EditCompanyResponse CreateResultForDuplicateRequest()
        {
            return Guid.Empty;
        }
    }

}
