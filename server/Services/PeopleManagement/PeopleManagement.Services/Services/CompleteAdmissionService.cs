#pragma warning disable CS0618 // Ultimo consumidor VIVO da feature Archive: o fluxo de
// admissao ainda cria e confere os arquivos por evento. A superficie HTTP foi removida em
// 2026-09-04, esta nao. Enquanto isto existir, Archive nao pode ser apagado do banco.
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;

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

        public async Task<Employee> CompleteAdmission(Guid employeeId, Guid companyId, Registration registration, DateOnly dateInit, EmploymentContractType contractType, CancellationToken cancellationToken = default)
        {
            var hasRequiresFiles = await _archiveService.HasRequiresFiles(employeeId, companyId);
            if (hasRequiresFiles)
                throw new DomainException(this, DomainErrors.Employee.HasRequiresFiles());

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            employee.CompleteAdmission(registration, dateInit, contractType);

            return employee;
        }
    }
}
