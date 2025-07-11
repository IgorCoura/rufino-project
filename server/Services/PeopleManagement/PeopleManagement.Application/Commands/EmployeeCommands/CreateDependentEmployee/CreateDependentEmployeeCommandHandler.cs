﻿
using PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public class CreateDependentEmployeeCommandHandler : IRequestHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateDependentEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<CreateDependentEmployeeResponse> Handle(CreateDependentEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var dependent = request.ToDependent();

            employee.AddDependent(dependent);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class CreateDependentEmployeeIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>(mediator, logger, requestManager)
    {
        protected override CreateDependentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
