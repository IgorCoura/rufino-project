
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee
{
    public sealed class AlterMilitarDocumentEmployeeCommandHandler : IRequestHandler<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterMilitarDocumentEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterMilitarDocumentEmployeeResponse> Handle(AlterMilitarDocumentEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var militarDocument = MilitaryDocument.Create(request.DocumentNumber, request.DocumentType);

            employee.MilitaryDocument = militarDocument;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
    public class AlterMilitarDocumentEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>(mediator, logger)
    {
        protected override AlterMilitarDocumentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }


}

