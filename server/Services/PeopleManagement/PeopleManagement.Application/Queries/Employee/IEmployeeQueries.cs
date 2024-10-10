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
        Task<EmployeeMilitaryDocumentDto> GetEmployeeMilitaryDocument(Guid id, Guid company);
        Task<EmployeeDependentsDto> GetEmployeeDependents(Guid id, Guid company);
    }
}
