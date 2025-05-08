using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class AssociationType : Enumeration
    {
        public static readonly AssociationType Role = new(1, nameof(Role));
        public static readonly AssociationType Workplace = new(2, nameof(Workplace));

        private AssociationType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator AssociationType(int id) => Enumeration.FromValue<AssociationType>(id);
        public static implicit operator AssociationType(string name) => Enumeration.FromDisplayName<AssociationType>(name);
    }
}
