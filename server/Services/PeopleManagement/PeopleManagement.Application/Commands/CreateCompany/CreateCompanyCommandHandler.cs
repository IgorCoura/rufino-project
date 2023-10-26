namespace PeopleManagement.Application.Commands.CreateCompany;
public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, BaseDTO>
{
    public Task<BaseDTO> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = Company.Create(
            request.CorporateNmae, 
            request.FantasyName, 
            request.Cnpj, 
            new Address(
                request.ZipCode, 
                request.Street, 
                request.Number, 
                request.Complement, 
                request.Neighborhood, 
                request.City, 
                request.State, 
                request.Country)
            );

        return Task.FromResult(new BaseDTO(company.Id));
    }
}
