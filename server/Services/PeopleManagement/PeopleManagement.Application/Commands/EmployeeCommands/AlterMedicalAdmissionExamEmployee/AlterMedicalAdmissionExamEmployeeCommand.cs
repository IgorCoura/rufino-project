namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee
{
    public record AlterMedicalAdmissionExamEmployeeCommand(Guid EmployeeId, Guid CompanyId, DateOnly DateExam, DateOnly ValidityExam) : IRequest<AlterMedicalAdmissionExamEmployeeResponse>
    {
    }
}
