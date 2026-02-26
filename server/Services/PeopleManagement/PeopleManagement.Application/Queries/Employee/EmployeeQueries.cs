using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Util;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Employee
{
    public class EmployeeQueries(IDbContextFactory<PeopleManagementContext> factory, IBlobService blobService) : IEmployeeQueries
    {
        private IDbContextFactory<PeopleManagementContext> _factory = factory;
        private IBlobService _blobService = blobService;

        public async Task<IEnumerable<EmployeeWithRoleAndDocumentStatusDto>> GetEmployeeListWithRolesAndDocumentStatus(EmployeeParams pms, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query =

                    from e in context.Employees
                    join r in context.Roles on e.RoleId equals r.Id into roleGroup
                    from r in roleGroup.DefaultIfEmpty()
                    select new
                    {
                        Employee = e,
                        Role = r
                    }
                ;

            query = query.Where(e => e.Employee.CompanyId == company);


            if (!string.IsNullOrEmpty(pms.Name))
            {
                query = query.Where(e => ((string)e.Employee.Name).Contains(pms.Name.ToUpper()));
            }

            if (!string.IsNullOrEmpty(pms.Role))
            {
                query = query.Where(e => ((string)e.Role.Name).Contains(pms.Role.ToUpper()));
            }

            if (pms.Status.HasValue && Enumeration.TryFromValue<Status>((int)pms.Status) != null)
            {
                query = query.Where(e => e.Employee.Status == (Status)pms.Status);
            }

            if (pms.DocumentRepresentingStatus.HasValue)
            {
                query = query.Where(e => e.Employee.DocumentRepresentingStatus.Id == pms.DocumentRepresentingStatus.Value);
            }

            query = pms.SortOrder == SortOrder.ASC
                ? query.OrderBy(e => e.Employee.Name)
                : query.OrderByDescending(e => e.Employee.Name);

            query = query.Skip(pms.SizeSkip).Take(pms.PageSize);

            var employees = await query.Select(o => new EmployeeWithRoleAndDocumentStatusDto
            {
                Id = o.Employee.Id,
                Name = o.Employee.Name.Value,
                Registration = o.Employee.Registration == null ? string.Empty : o.Employee.Registration!.Value,
                Status = new EnumerationDto
                {
                    Id = o.Employee.Status.Id,
                    Name = o.Employee.Status.Name,
                },
                RoleId = o.Employee.RoleId,
                RoleName = o.Role.Name.Value == null ? string.Empty : o.Role.Name.Value,
                CompanyId = o.Employee.CompanyId,
                WorkplaceId = o.Employee.WorkPlaceId,
                DocumentRepresentingStatus = new EnumerationDto
                {
                    Id = o.Employee.DocumentRepresentingStatus.Id,
                    Name = o.Employee.DocumentRepresentingStatus.Name,
                }
            }).ToListAsync();

            return employees;
        }

        public async Task<EmployeeDto> GetEmployee(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeDto
            {
                Id = o.Id,
                Name = o.Name.Value,
                Registration = o.Registration == null ? string.Empty : o.Registration!.Value,
                Status = new EnumerationDto
                {
                    Id = o.Status.Id,
                    Name = o.Status.Name,
                },
                DocumentSigningOptions = new EnumerationDto
                {
                    Id = o.DocumentSigningOptions.Id,
                    Name = o.DocumentSigningOptions.Name,
                },
                CompanyId = o.CompanyId,
                RoleId = o.RoleId,
                WorkplaceId = o.WorkPlaceId
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeContactDto> GetEmployeeContact(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeContactDto
            {
                EmployeeId = o.Id,
                Email = o.Contact == null ? string.Empty : o.Contact.Email,
                Cellphone = o.Contact == null ? string.Empty : o.Contact.CellPhone,
                CompanyId = o.CompanyId
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeAddressDto> GetEmployeeAddress(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeAddressDto
            {
                EmployeeId = o.Id,
                Zipcode = o.Address == null ? string.Empty : o.Address.ZipCode,
                Street = o.Address == null ? string.Empty : o.Address.Street,
                Number = o.Address == null ? string.Empty : o.Address.Number,
                Complement = o.Address == null ? string.Empty : o.Address.Complement,
                Neighborhood = o.Address == null ? string.Empty : o.Address.Neighborhood,
                City = o.Address == null ? string.Empty : o.Address.City,
                State = o.Address == null ? string.Empty : o.Address.State,
                Coutry = o.Address == null ? string.Empty : o.Address.Country,
                CompanyId = o.CompanyId
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeePersonalInfoDto> GetEmployeePersonalInfo(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = await context.Employees.Where(e => e.Id == id && e.CompanyId == company).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            var result = new EmployeePersonalInfoDto
            {
                EmployeeId = query.Id,
                CompanyId = query.CompanyId,
                Deficiency = query.PersonalInfo != null ? new EmployeeDeficiencyDto
                {
                    Disabilities = query.PersonalInfo.Deficiency.Disabilities.Select(x => new EnumerationDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToArray(),
                    Observation = query.PersonalInfo.Deficiency.Observation,
                } : EmployeeDeficiencyDto.Empty,
                MaritalStatus = query.PersonalInfo != null ? new EnumerationDto { Id = query.PersonalInfo.MaritalStatus.Id, Name = query.PersonalInfo.MaritalStatus.Name } : EnumerationDto.Empty,
                Gender = query.PersonalInfo != null ? new EnumerationDto { Id = query.PersonalInfo.Gender.Id, Name = query.PersonalInfo.Gender.Name } : EnumerationDto.Empty,
                Ethinicity = query.PersonalInfo != null ? new EnumerationDto { Id = query.PersonalInfo.Ethinicity.Id, Name = query.PersonalInfo.Ethinicity.Name } : EnumerationDto.Empty,
                EducationLevel = query.PersonalInfo != null ? new EnumerationDto { Id = query.PersonalInfo.EducationLevel.Id, Name = query.PersonalInfo.EducationLevel.Name } : EnumerationDto.Empty,

            };

            return result;
        }

        public async Task<EmployeeIdCardDto> GetEmployeeIdCard(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeIdCardDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                IdCard = o.IdCard == null ? IdCardDto.Empty : new IdCardDto
                {
                    Cpf = o.IdCard!.Cpf.Number,
                    MotherName = o.IdCard.MotherName.Value,
                    FatherName = o.IdCard.FatherName.Value,
                    BirthCity = o.IdCard.BirthCity,
                    BirthState = o.IdCard.BirthState,
                    Nacionality = o.IdCard.Nacionality,
                    DateOfBirth = o.IdCard.DateOfBirth,
                },
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeVoteIdDto> GetEmployeeVoteId(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeVoteIdDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                VoteIdNumber = o.VoteId != null ? o.VoteId.Number : string.Empty,
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeMilitaryDocumentDto> GetEmployeeMilitaryDocument(Guid id, Guid company, bool isRequired)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeMilitaryDocumentDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                Number = o.MilitaryDocument != null ? o.MilitaryDocument.Number : string.Empty,
                Type = o.MilitaryDocument != null ? o.MilitaryDocument.Type : string.Empty,
                IsRequired = isRequired == false && o.MilitaryDocument != null || isRequired
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeDependentsDto> GetEmployeeDependents(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeDependentsDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                Dependents = o.Dependents.Select(dep => new EmployeeDependentDto
                {
                    Name = dep.Name,
                    IdCard = new IdCardDto
                    {
                        Cpf = dep.IdCard.Cpf.Number,
                        MotherName = dep.IdCard.MotherName.Value,
                        FatherName = dep.IdCard.FatherName.Value,
                        BirthCity = dep.IdCard.BirthCity,
                        BirthState = dep.IdCard.BirthState,
                        Nacionality = dep.IdCard.Nacionality,
                        DateOfBirth = dep.IdCard.DateOfBirth,
                    },
                    Gender = new EnumerationDto
                    {
                        Id = dep.Gender.Id,
                        Name = dep.Gender.Name,
                    },
                    DependencyType = new EnumerationDto
                    {
                        Id = dep.DependencyType.Id,
                        Name = dep.DependencyType.Name,
                    },
                }).ToArray(),
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<MedicalAdmissionExamDto> GetEmployeeMedicalAdmissionExam(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new MedicalAdmissionExamDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                DateExam = o.MedicalAdmissionExam != null ? o.MedicalAdmissionExam.DateExam : null,
                ValidityExam = o.MedicalAdmissionExam != null ? o.MedicalAdmissionExam.Validity : null,
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeContractsDto> GetEmployeeContracts(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Employees.Where(e => e.Id == id && e.CompanyId == company);

            var result = await query.Select(o => new EmployeeContractsDto
            {
                EmployeeId = o.Id,
                CompanyId = o.CompanyId,
                Contracts = o.Contracts.Select(c => new EmployeeContractDto{
                    InitDate = c.InitDate,
                    FinalDate = c.FinalDate,
                    Type = new EnumerationDto
                    {
                        Id = c.ContractType.Id,
                        Name = c.ContractType.Name,
                    },
                }).ToArray(),
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<EmployeeImageDto> DownloadImage(Guid id, Guid company)
        {
            using var context = _factory.CreateDbContext();
            var employee = await context.Employees.Where(e => e.Id == id && e.CompanyId == company).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            var img = employee.GetImage();

            Stream? file = await _blobService.DownloadAsync(img.GetNameWithExtension, employee.CompanyId.ToString());

            return new EmployeeImageDto { 
                stream = file,
                Extension = img.Extension.ToString(),
            };
        }
    }
}
