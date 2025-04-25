using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class AssociationType : Enumeration
    {
        public static readonly AssociationType Role = new(1, nameof(Role), typeof(Role));
        public static readonly AssociationType Workplace = new(2, nameof(Workplace), typeof(Workplace));

        public Type Type { get; private set; }
        private AssociationType(int id, string name, Type type) : base(id, name)
        {
            Type = type;
        }

        public static implicit operator AssociationType(int id) => Enumeration.FromValue<AssociationType>(id);
        public static implicit operator AssociationType(string name) => Enumeration.FromDisplayName<AssociationType>(name);
    }
}
