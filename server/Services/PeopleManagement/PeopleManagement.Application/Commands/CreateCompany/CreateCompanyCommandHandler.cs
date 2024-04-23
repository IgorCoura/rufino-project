using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.Application.Commands.CreateCompany;
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
        var company = Company.Create(
            request.CorporateName!, 
            request.FantasyName!, 
            request.Description!,
            request.Cnpj!, 
            request.Email!,
            request.Phone!,
            Address.Create(
                request.ZipCode!, 
                request.Street!, 
                request.Number!, 
                request.Complement!, 
                request.Neighborhood!, 
                request.City!, 
                request.State!, 
                request.Country!)
            );

        var result = await _companyReposity.InsertAsync( company );

        await _companyReposity.UnitOfWork.SaveChangesAsync( cancellationToken );

        return new BaseDTO(result.Id);
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
