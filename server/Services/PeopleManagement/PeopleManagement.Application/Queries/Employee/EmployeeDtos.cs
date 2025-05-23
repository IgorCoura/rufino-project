using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Employee
{
    public record EmployeeWithRoleAndDocumentStatusDto
    {

        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Registration { get; init; }
        public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
        public Guid? RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
        public Guid CompanyId { get; init; }
        public Guid? WorkplaceId { get; init; }
        public EnumerationDto DocumentRepresentingStatus { get; init; } = EnumerationDto.Empty;
    }

    public record EmployeeDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
        public string? Registration { get; init; }
        public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
        public Guid? RoleId { get; init; }
        public Guid CompanyId { get; init; }
        public Guid? WorkplaceId { get; init; }
    }
    public record EmployeeParams
    {
        public string? Name { get; init; }
        public string? Role { get; init; }
        public int? Status { get; init; }
        public int PageSize { get; init; } = 10;
        public int SizeSkip { get; init; } = 0;
        public SortOrder SortOrder { get; init; } = SortOrder.ASC;
    }

    public enum SortOrder
    {
        ASC,
        DESC,
    }

    public record EmployeeContactDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Cellphone { get; init; } = string.Empty;
    }

    public record EmployeeAddressDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public string Zipcode { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;
        public string Number { get; init; } = string.Empty;
        public string Complement { get; init; } = string.Empty;
        public string Neighborhood { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string Coutry { get; init; } = string.Empty;
    }

    public record EmployeePersonalInfoDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public EmployeeDeficiencyDto Deficiency { get; init; } = EmployeeDeficiencyDto.Empty;
        public EnumerationDto MaritalStatus { get; init; } = EnumerationDto.Empty;
        public EnumerationDto Gender { get; init; } = EnumerationDto.Empty;
        public EnumerationDto Ethinicity { get; init; } = EnumerationDto.Empty;
        public EnumerationDto EducationLevel { get; init; } = EnumerationDto.Empty;
    }

    public record EmployeeDeficiencyDto
    {
        public EnumerationDto[] Disabilities { get; init; } = [];
        public string Observation { get; init; } = string.Empty;

        public static EmployeeDeficiencyDto Empty => new();
    }

    public record EmployeeIdCardDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public IdCardDto IdCard { get; init; } = IdCardDto.Empty;
    }

    public record IdCardDto
    {
        public string Cpf { get; init; } = string.Empty;
        public string MotherName { get; init; } = string.Empty;
        public string FatherName { get; init; } = string.Empty;
        public string BirthCity { get; init; } = string.Empty;
        public string BirthState { get; init; } = string.Empty;
        public string Nacionality { get; init; } = string.Empty;
        public DateOnly? DateOfBirth { get; init; } = null;

        public static IdCardDto Empty => new();
    }


    public record EmployeeVoteIdDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public string VoteIdNumber { get; init; } = string.Empty;

    }

    public record EmployeeMilitaryDocumentDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public string Number { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public bool IsRequired { get; init; } = false;

    }

    public record EmployeeDependentsDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public EmployeeDependentDto[] Dependents { get; init; } = [];
    }

    public record EmployeeDependentDto
    {
        public string Name { get; init; } = string.Empty;
        public IdCardDto IdCard { get; init; } = IdCardDto.Empty;
        public EnumerationDto Gender { get; init; } = EnumerationDto.Empty;
        public EnumerationDto DependencyType { get; init; } = EnumerationDto.Empty;
    }


    public record MedicalAdmissionExamDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public DateOnly? DateExam { get; init; }
        public DateOnly? ValidityExam { get; init; }

    }

    public record EmployeeContractsDto
    {
        public Guid EmployeeId { get; init; }
        public Guid CompanyId { get; init; }
        public EmployeeContractDto[] Contracts { get; init; } = [];
    }

    public record EmployeeContractDto
    {
        public DateOnly InitDate { get; init; }
        public DateOnly? FinalDate { get; init; }
        public EnumerationDto Type { get; init; } = EnumerationDto.Empty;

    }
}
