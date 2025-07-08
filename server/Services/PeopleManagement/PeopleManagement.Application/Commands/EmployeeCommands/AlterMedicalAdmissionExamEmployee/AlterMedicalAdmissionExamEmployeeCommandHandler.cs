
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee
{
    public sealed class AlterMedicalAdmissionExamEmployeeCommandHandler : IRequestHandler<AlterMedicalAdmissionExamEmployeeCommand, AlterMedicalAdmissionExamEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterMedicalAdmissionExamEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterMedicalAdmissionExamEmployeeResponse> Handle(AlterMedicalAdmissionExamEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));


            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(request.DateExam, request.ValidityExam);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
    public class AlterMedicalAdmissionExamEmployeeIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<AlterMedicalAdmissionExamEmployeeCommand, AlterMedicalAdmissionExamEmployeeResponse>> logger, 
        IRequestManager requestManager) 
        : IdentifiedCommandHandler<AlterMedicalAdmissionExamEmployeeCommand, AlterMedicalAdmissionExamEmployeeResponse>(mediator, logger, requestManager)
    {
        protected override AlterMedicalAdmissionExamEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }


}

