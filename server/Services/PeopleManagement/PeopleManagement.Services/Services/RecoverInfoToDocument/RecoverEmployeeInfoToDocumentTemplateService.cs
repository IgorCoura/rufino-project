using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.Json;
using System.Text.Json.Nodes;
using static PeopleManagement.Domain.ErrorTools.ErrorsMessages.DomainErrors;
using Employee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Employee;
using Contact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;
using Address = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverEmployeeInfoToDocumentTemplateService(IEmployeeRepository employeeRepository) : IRecoverEmployeeInfoToDocumentTemplateService
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, JsonObject[]? jsonObjects = null, CancellationToken cancellation = default)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var employeeJson = new JsonObject
            {
                ["Employee"] = new JsonObject
                {
                    ["Id"] = employee.Id.ToString(),
                    ["Registration"] = employee.Registration?.ToString() ?? "",
                    ["Name"] = employee.Name?.ToString(),
                    ["Address"] = ConvertAddressToJsonObject(employee.Address),
                    ["Contact"] = ConvertContactToJsonObject(employee.Contact),
                    ["Status"] = employee.Status.ToString(),
                    ["MedicalAdmissionExam"] = ConvertMedicalAdmissionExamToJsonObject(employee.MedicalAdmissionExam),
                    ["PersonalInfo"] = ConvertPersonalInfoToJsonObject(employee.PersonalInfo),
                    ["IdCard"] = ConvertIdCardToJsonObject(employee.IdCard),
                    ["VoteId"] = ConvertVoteIdToJsonObject(employee.VoteId),
                    ["MilitaryDocument"] = ConvertMilitaryDocumentToJsonObject(employee.MilitaryDocument)
                    ["InitialDate"] = employee.Contracts.Where(x => x.IsActive).OrderByDescending(x => x.InitDate).FirstOrDefault()?.InitDate.ToString("dd-MM-yyyy") ?? "",
                }
            };

            return employeeJson;
        }

        public static JsonObject GetModel()
        {
            var address = Address.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");
            var contact = Contact.Create("email@email.com", "(00) 100000001");
            var personalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            var idCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.FromDateTime(DateTime.Now.AddYears(-20)));
            var voteId = VoteId.Create("281662310124");
            var medicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(365)));
            var militaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            var json = new JsonObject
            {
                ["Employee"] = new JsonObject
                {
                    ["Id"] = Guid.Empty.ToString(),
                    ["Registration"] = "employee.Registration",
                    ["Name"] = "employee.Name",
                    ["Address"] = ConvertAddressToJsonObject(address),
                    ["Contact"] = ConvertContactToJsonObject(contact),
                    ["Status"] = "employee.Status",
                    ["MedicalAdmissionExam"] = ConvertMedicalAdmissionExamToJsonObject(medicalAdmissionExam),
                    ["PersonalInfo"] = ConvertPersonalInfoToJsonObject(personalInfo),
                    ["IdCard"] = ConvertIdCardToJsonObject(idCard),
                    ["VoteId"] = ConvertVoteIdToJsonObject(voteId),
                    ["MilitaryDocument"] = ConvertMilitaryDocumentToJsonObject(militaryDocument)
                }
            };

            return json;
        }

        private static JsonObject ConvertAddressToJsonObject(Address? address)
        {
            if(address == null)
            {
                return new JsonObject
                {
                };
            }

            return new JsonObject
            {
                ["ZipCode"] = address.ZipCode,
                ["Street"] = address.Street,
                ["Number"] = address.Number,
                ["Complement"] = address.Complement,
                ["Neighborhood"] = address.Neighborhood,
                ["City"] = address.City,
                ["State"] = address.State,
                ["Country"] = address.Country
            };
        }
        private static JsonObject ConvertContactToJsonObject(Contact? contact)
        {
            if (contact == null)
            {
                return new JsonObject
                {
                };
            }

            return new JsonObject
            {
                ["Email"] = contact.Email,
                ["CellPhone"] = contact.CellPhone
            };
        }
        private static JsonObject ConvertPersonalInfoToJsonObject(PersonalInfo? personalInfo)
        {
            if (personalInfo == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["Deficiency"] = ConvertDeficiencyToJsonObject(personalInfo.Deficiency),
                ["MaritalStatus"] = personalInfo.MaritalStatus.ToString(),
                ["Gender"] = personalInfo.Gender.ToString(),
                ["Ethnicity"] = personalInfo.Ethinicity.Name,
                ["EducationLevel"] = personalInfo.EducationLevel.ToString()
            };
        }
        private static JsonObject ConvertDeficiencyToJsonObject(Deficiency? deficiency)
        {
            if (deficiency == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["Observation"] = deficiency.Observation,
                ["Disabilities"] = JsonSerializer.Serialize(deficiency.Disabilities)
            };

        }
        private static JsonObject ConvertIdCardToJsonObject(IdCard? idCard)
        {
            if (idCard == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["CPF"] = idCard.Cpf.ToString(),
                ["FatherName"] = idCard.FatherName.ToString(),
                ["MotherName"] = idCard.MotherName.ToString(),
                ["BirthCity"] = idCard.BirthCity.ToString(),
                ["BirthState"] = idCard.BirthState.ToString(),
                ["Nationality"] = idCard.Nacionality.ToString(),
                ["DateOfBirth"] = idCard.DateOfBirth.ToString("yyyy-MM-dd")
            };
        }
        private static JsonObject ConvertVoteIdToJsonObject(VoteId? voteId)
        {
            if (voteId == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["VoteIdNumber"] = voteId.Number
            };

        }
        private static JsonObject ConvertMilitaryDocumentToJsonObject(MilitaryDocument? militaryDocument)
        {
            if (militaryDocument == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["Number"] = militaryDocument.Number,
                ["Type"] = militaryDocument.Type
            };
        }
        private static JsonObject ConvertMedicalAdmissionExamToJsonObject(MedicalAdmissionExam? medicalAdmissionExam)
        {
            if (medicalAdmissionExam == null)
            {
                return new JsonObject
                {
                };
            }
            return new JsonObject
            {
                ["DateExam"] = medicalAdmissionExam.DateExam.ToString("yyyy-MM-dd"),
                ["Validity"] = medicalAdmissionExam.Validity.ToString("yyyy-MM-dd")
            };
        }
    }
}
