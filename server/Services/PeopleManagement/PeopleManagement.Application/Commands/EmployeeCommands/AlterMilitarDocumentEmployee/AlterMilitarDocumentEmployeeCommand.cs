using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee
{
    public record AlterMilitarDocumentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string DocumentNumber, string DocumentType) : IRequest<AlterMilitarDocumentEmployeeResponse>
    {
        public MilitaryDocument ToMilitaryDocument() => MilitaryDocument.Create(DocumentNumber, DocumentType);
    }

    public record AlterMilitarDocumentEmployeeModel(Guid EmployeeId, string DocumentNumber, string DocumentType) : IRequest<AlterMilitarDocumentEmployeeResponse>
    {
        public AlterMilitarDocumentEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, DocumentNumber, DocumentType);
    }
}
