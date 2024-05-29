
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee
{
    public sealed class AlterIdCardEmployeeCommandHandler : IRequestHandler<AlterIdCardEmployeeCommand, AlterIdCardEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterIdCardEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterIdCardEmployeeResponse> Handle(AlterIdCardEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.IdCard = request.ToIdCard();   

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterIdCardEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterIdCardEmployeeCommand, AlterIdCardEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterIdCardEmployeeCommand, AlterIdCardEmployeeResponse>(mediator, logger)
    {
        protected override AlterIdCardEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}


