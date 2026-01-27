
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterImageEmployee
{
    public class AlterImageEmployeeCommandHandler(IEmployeeRepository employeeRepository, IBlobService blobService) : IRequestHandler<AlterImageEmployeeCommand, AlterImageEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IBlobService _blobService = blobService;

        public async Task<AlterImageEmployeeResponse> Handle(AlterImageEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.SetImage(request.Extension);

            var img = employee.GetImage();

            await _blobService.UploadAsync(request.Stream, img.GetNameWithExtension, employee.CompanyId.ToString(), overwrite: true, cancellationToken: cancellationToken); 

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterImageEmployeeIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<AlterImageEmployeeCommand, AlterImageEmployeeResponse>> logger,
        IRequestManager requestManager)
        : IdentifiedCommandHandler<AlterImageEmployeeCommand, AlterImageEmployeeResponse>(mediator, logger, requestManager)
    {
        protected override AlterImageEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
