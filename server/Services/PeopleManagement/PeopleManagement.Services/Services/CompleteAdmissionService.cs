using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Services.Services
{
    public class CompleteAdmissionService : ICompleteAdmissionService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IArchiveService _archiveService;
        public CompleteAdmissionService(IEmployeeRepository employeeRepository, IArchiveService archiveService)
        {
            _employeeRepository = employeeRepository;
            _archiveService = archiveService;
        }

        public async Task<Employee> CompleteAdmission(Guid employeeId, Guid companyId, Registration registration, EmploymentContactType contractType, CancellationToken cancellationToken = default)
        {
            var hasRequiresFiles = await _archiveService.HasRequiresFiles(employeeId, companyId);
            if (hasRequiresFiles)
                throw new DomainException(this, DomainErrors.Employee.HasRequiresFiles());

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            employee.CompleteAdmission(registration, contractType);

            return employee;
        }
    }
}
