using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class RecoverDataType : Enumeration
    {
        private const string Header = "header.html";
        private const string Body = "index.html";
        private const string Footer = "footer.html";

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

  

        public string GetSourcePath(string source)
        {
            return Path.Combine(source, TemplateName);
        }
        public string GetHeaderPath(string source)
        {
            return Path.Combine(source, TemplateName, Header);
        }

        public string GetBodyPath(string source)
        {
            return Path.Combine(source, TemplateName, Body);
        }

        public string GetFooterPath(string source)
        {
            return Path.Combine(source, TemplateName, Footer);
        }
    }
}
