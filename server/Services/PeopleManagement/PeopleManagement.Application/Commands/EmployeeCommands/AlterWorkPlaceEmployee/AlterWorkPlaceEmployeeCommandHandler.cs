
using PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public sealed class AlterWorkPlaceEmployeeCommandHandler : IRequestHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterWorkPlaceEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterWorkPlaceEmployeeResponse> Handle(AlterWorkPlaceEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.WorkPlaceId = request.WorkPlaceId;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterWorkPlaceEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>(mediator, logger)
    {
        protected override AlterWorkPlaceEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
