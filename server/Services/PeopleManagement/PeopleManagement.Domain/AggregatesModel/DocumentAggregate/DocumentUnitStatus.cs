namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public sealed class DocumentUnitStatus : Enumeration
    {
        public static readonly DocumentUnitStatus Pending = new(1, nameof(Pending));
        public static readonly DocumentUnitStatus OK = new(2, nameof(OK));
        public static readonly DocumentUnitStatus Deprecated = new(3, nameof(Deprecated));
        public static readonly DocumentUnitStatus Invalid = new(4, nameof(Invalid));
        public static readonly DocumentUnitStatus RequiresValidation = new(5, nameof(RequiresValidation));
        public static readonly DocumentUnitStatus NotApplicable = new(6, nameof(NotApplicable));
        public static readonly DocumentUnitStatus AwaitingSignature = new (7, nameof(AwaitingSignature));
        private DocumentUnitStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentUnitStatus(int id) => Enumeration.FromValue<DocumentUnitStatus>(id);
        public static implicit operator DocumentUnitStatus(string name) => Enumeration.FromDisplayName<DocumentUnitStatus>(name);
    }
}
