﻿using PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee
{
    public sealed class CompleteAdmissionEmployeeCommandHandler : IRequestHandler<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>
    {
        private readonly ICompleteAdmissionService _completeAdmissionService;
        private readonly IEmployeeRepository _employeeRepository;

        public CompleteAdmissionEmployeeCommandHandler(ICompleteAdmissionService completeAdmissionService, IEmployeeRepository employeeRepository)
        {
            _completeAdmissionService = completeAdmissionService;
            _employeeRepository = employeeRepository;
        }

        public async Task<CompleteAdmissionEmployeeResponse> Handle(CompleteAdmissionEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _completeAdmissionService.CompleteAdmission(request.EmployeeId, request.CompanyId, 
                request.Registration, request.ContractType, cancellationToken);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterWorkPlaceEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>(mediator, logger)
    {
        protected override AlterWorkPlaceEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}