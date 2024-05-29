using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee
{
    public record AlterMilitarDocumentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string DocumentNumber, string DocumentType) : IRequest<AlterMilitarDocumentEmployeeResponse>
    {
        public MilitaryDocument ToMilitaryDocument() => MilitaryDocument.Create(DocumentNumber, DocumentType);
    }
}
