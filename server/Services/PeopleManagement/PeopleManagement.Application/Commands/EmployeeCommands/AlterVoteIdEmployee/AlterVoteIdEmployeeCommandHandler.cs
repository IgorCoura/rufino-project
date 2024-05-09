
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee
{
    public sealed class AlterVoteIdEmployeeCommandHandler : IRequestHandler<AlterVoteIdEmployeeCommand, AlterVoteIdEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterVoteIdEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterVoteIdEmployeeResponse> Handle(AlterVoteIdEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.VoteId = request.VoteIdNumber;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
