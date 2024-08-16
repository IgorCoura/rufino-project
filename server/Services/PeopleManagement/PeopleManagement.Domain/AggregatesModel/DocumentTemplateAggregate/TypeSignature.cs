namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class TypeSignature : Enumeration
    {
        public static readonly TypeSignature Signature = new(1, nameof(Signature));
        public static readonly TypeSignature Visa = new(2, nameof(Visa));
        private TypeSignature(int id, string name) : base(id, name)
        {
        }

        public static implicit operator TypeSignature(int id) => Enumeration.FromValue<TypeSignature>(id);
        public static implicit operator TypeSignature(string name) => Enumeration.FromDisplayName<TypeSignature>(name);
    }
}
