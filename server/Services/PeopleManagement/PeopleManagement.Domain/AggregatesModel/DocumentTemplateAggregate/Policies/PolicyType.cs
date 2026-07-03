using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Discriminador das regras (policies) que um <see cref="DocumentTemplate"/> pode compor.
    /// Smart enum no mesmo estilo de RecoverDataType — persistido como int e reidratado por Id.
    /// </summary>
    public class PolicyType : Enumeration
    {
        public static readonly PolicyType Expiration = new(1, nameof(Expiration));
        public static readonly PolicyType Period = new(2, nameof(Period));
        public static readonly PolicyType Generation = new(3, nameof(Generation));
        public static readonly PolicyType Workload = new(4, nameof(Workload));
        public static readonly PolicyType Signature = new(5, nameof(Signature));

        private PolicyType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator PolicyType(int id) => CreateFromValue(id);

        public static PolicyType CreateFromValue(int value)
        {
            try
            {
                return FromValue<PolicyType>(value);
            }
            catch
            {
                throw new DomainException(nameof(PolicyType), DomainErrors.ErroCreateEnumeration(nameof(PolicyType), value.ToString()));
            }
        }
    }
}
