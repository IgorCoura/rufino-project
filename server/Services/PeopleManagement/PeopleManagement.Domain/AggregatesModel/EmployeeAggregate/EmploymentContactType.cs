namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class EmploymentContactType : Enumeration
    {
        public static readonly EmploymentContactType CLT = new (1, nameof(CLT));
        public static readonly EmploymentContactType Aprendiz = new (2, nameof(Aprendiz));
        public static readonly EmploymentContactType VerdeAmarelo = new (3, nameof(VerdeAmarelo));
        public static readonly EmploymentContactType VerdeAmareloComAcordoFGTS = new (4, nameof(VerdeAmareloComAcordoFGTS));
        private EmploymentContactType(int id, string name) : base(id, name)
        {
        }
    }
}
