

namespace PeopleManagement.Application.Commands.CompanyCommands.EditCompany
{
    public record EditCompanyResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditCompanyResponse(Guid id) => new(id);
    }
}
