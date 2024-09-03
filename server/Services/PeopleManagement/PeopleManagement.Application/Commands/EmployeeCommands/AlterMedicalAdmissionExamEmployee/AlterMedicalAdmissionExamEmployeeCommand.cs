using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee
{
    public record AlterMedicalAdmissionExamEmployeeCommand(Guid EmployeeId, Guid CompanyId, DateOnly DateExam, DateOnly ValidityExam) : IRequest<AlterMedicalAdmissionExamEmployeeResponse>
    {
        public MedicalAdmissionExam ToMedicalAdmissionExam() => MedicalAdmissionExam.Create(DateExam, ValidityExam);
    }

    public record AlterMedicalAdmissionExamEmployeeModel(Guid EmployeeId, DateOnly DateExam, DateOnly ValidityExam)
    {
        public AlterMedicalAdmissionExamEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, DateExam, ValidityExam);
    }
}
