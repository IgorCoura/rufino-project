using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee
{
    public record AlterPersonalInfoEmployeeCommand(Guid EmployeeId, Guid CompanyId, DeficiencyModel Deficiency,  int MaritalStatus, int Gender, int Ethinicity, int EducationLevel) : IRequest<AlterPersonalInfoEmployeeResponse>
    {
        public PersonalInfo ToPersonalInfo() => PersonalInfo.Create(Deficiency.ToDeficiency(), MaritalStatus, Gender, Ethinicity, EducationLevel);
    }
    public record DeficiencyModel(int[] Disability, string Observation)
    {
        public Deficiency ToDeficiency() => Deficiency.Create(Observation, Disability.Select(x => (Disability)x).ToArray());
    }
}
