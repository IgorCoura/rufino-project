namespace PeopleManagement.Application.Queries.Employee
{
    public interface IEmployeeQueries
    {
        Task<IEnumerable<EmployeeWithRoleDto>> GetEmployeeListWithRoles(EmployeeParams pms, Guid company);
        Task<EmployeeDto> GetEmployee(Guid id, Guid company);
        Task<EmployeeContactDto> GetEmployeeContact(Guid id, Guid company);
        Task<EmployeeAddressDto> GetEmployeeAddress(Guid id, Guid company);
        Task<EmployeePersonalInfoDto> GetEmployeePersonalInfo(Guid id, Guid company);
        Task<EmployeeIdCardDto> GetEmployeeIdCard(Guid id, Guid company);
        Task<EmployeeVoteIdDto> GetEmployeeVoteId(Guid id, Guid company);
        Task<EmployeeMilitaryDocumentDto> GetEmployeeMilitaryDocument(Guid id, Guid company, bool isRequired);
        Task<EmployeeDependentsDto> GetEmployeeDependents(Guid id, Guid company);
        Task<MedicalAdmissionExamDto> GetEmployeeMedicalAdmissionExam(Guid id, Guid company);
        Task<EmployeeContractsDto> GetEmployeeContracts(Guid id, Guid company);
    }
}
