using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class AssociationType : Enumeration
    {
        public static readonly AssociationType Role = new(1, nameof(Role), typeof(Role));

        public string TemplateName { get; private set; }
        public Type Type { get; private set; }
        private AssociationType(int id, string name, Type type) : base(id, name)
        {
            Type = type;
        }

        public static implicit operator AssociationType(int id) => Enumeration.FromValue<AssociationType>(id);
        public static implicit operator AssociationType(string name) => Enumeration.FromDisplayName<AssociationType>(name);
    }
}
