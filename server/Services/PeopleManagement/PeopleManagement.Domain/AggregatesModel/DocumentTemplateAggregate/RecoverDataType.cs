using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class RecoverDataType : Enumeration
    {

        public static readonly RecoverDataType NR01 = new(1, nameof(NR01), "NR01", typeof(IRecoverNR01InfoToDocumentTemplateService));

        public string TemplateName { get; private set; }
        public Type Type { get; private set; }
        private RecoverDataType(int id, string name, string templateName, Type type) : base(id, name)
        {
            TemplateName = templateName;
            Type = type;
        }

        public static implicit operator RecoverDataType(int id) => Enumeration.FromValue<RecoverDataType>(id);
        public static implicit operator RecoverDataType(string name) => Enumeration.FromDisplayName<RecoverDataType>(name);

  
    }
}
