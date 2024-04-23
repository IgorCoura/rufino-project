namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class EmploymentContactType : Enumeration
    {
        public static readonly EmploymentContactType CLT = new (1, nameof(CLT));
        public static readonly EmploymentContactType Aprendiz = new (2, nameof(Aprendiz));
        public static readonly EmploymentContactType VerdeAmareloComAcordoFGTS = new (3, nameof(VerdeAmareloComAcordoFGTS));
        public static readonly EmploymentContactType VerdeAmareloSemAcordoFGTS = new (4, nameof(VerdeAmareloSemAcordoFGTS));
        private EmploymentContactType(int id, string name) : base(id, name)
        {
        }
    }
}
