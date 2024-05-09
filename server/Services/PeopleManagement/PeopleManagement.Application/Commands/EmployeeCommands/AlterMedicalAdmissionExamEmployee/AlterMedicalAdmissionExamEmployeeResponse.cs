namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee
{
    public record AlterMedicalAdmissionExamEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterMedicalAdmissionExamEmployeeResponse(Guid id) => new(id);
    }
}
