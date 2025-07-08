namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class EmploymentContractType : Enumeration
    {
        public static readonly EmploymentContractType CLT = new (1, nameof(CLT));
        public static readonly EmploymentContractType Aprendiz = new (2, nameof(Aprendiz));
        public static readonly EmploymentContractType VerdeAmarelo = new (3, nameof(VerdeAmarelo));
        public static readonly EmploymentContractType VerdeAmareloComAcordoFGTS = new (4, nameof(VerdeAmareloComAcordoFGTS));
        private EmploymentContractType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator EmploymentContractType(int id) => Enumeration.FromValue<EmploymentContractType>(id);
        public static implicit operator EmploymentContractType(string name) => Enumeration.FromDisplayName<EmploymentContractType>(name);
    }
}
