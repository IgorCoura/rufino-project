using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee
{
    public class AlterNameEmployeeCommandHandler : IRequestHandler<AlterNameEmployeeCommand, AlterNameEmployeeResponse>
    {
        public readonly IEmployeeRepository _employeeRepository;

        public AlterNameEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterNameEmployeeResponse> Handle(AlterNameEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.Name = request.Name;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
    public class AlterNameEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterNameEmployeeCommand, AlterNameEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterNameEmployeeCommand, AlterNameEmployeeResponse>(mediator, logger)
    {
        protected override AlterNameEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}

