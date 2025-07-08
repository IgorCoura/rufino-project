using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
public class CreateCompanyCommandHandler(ICompanyRepository companyReposity, ILogger<CreateCompanyCommandHandler> logger) : IRequestHandler<CreateCompanyCommand, BaseDTO>
{
    private readonly ICompanyRepository _companyReposity = companyReposity;
    private readonly ILogger<CreateCompanyCommandHandler> _logger = logger;

    public async Task<BaseDTO> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = request.ToCompany(Guid.NewGuid());

        var result = await _companyReposity.InsertAsync(company, cancellationToken);

        await _companyReposity.UnitOfWork.SaveChangesAsync(cancellationToken);

        return result.Id;
    }
}


public class CreateCompanyIdentifiedCommandHandler : IdentifiedCommandHandler<CreateCompanyCommand, BaseDTO>
{
    public CreateCompanyIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateCompanyCommand, BaseDTO>> logger, IRequestManager requestManager) 
        : base(mediator, logger, requestManager)
    {
    }

    protected override BaseDTO CreateResultForDuplicateRequest()
    {
        return new BaseDTO(Guid.Empty);
    }
}
