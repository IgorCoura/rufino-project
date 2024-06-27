using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, BaseDTO>
{
    private readonly ICompanyRepository _companyReposity;
    private readonly ILogger<CreateCompanyCommandHandler> _logger;

    public CreateCompanyCommandHandler(ICompanyRepository companyReposity, ILogger<CreateCompanyCommandHandler> logger)
    {
        _companyReposity = companyReposity;
        _logger = logger;
    }

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
    public CreateCompanyIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateCompanyCommand, BaseDTO>> logger) : base(mediator, logger)
    {
    }

    protected override BaseDTO CreateResultForDuplicateRequest()
    {
        return new BaseDTO(Guid.Empty);
    }
}
