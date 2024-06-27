using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.SeedWord;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class DocumentType : Enumeration
    {
        private const string Header = "header.html";
        private const string Body = "body.html";
        private const string Footer = "footer.html";

        public static DocumentType NR01 = new(1, nameof(NR01), "NR01", typeof(IRecoverNR01InfoToSegurityDocumentService),  TimeSpan.FromDays(365));
        public string TemplateName { get; private set; }
        public TimeSpan? ValidityInterval { get; private set; }
        public Type Type { get; private set; }
        public DocumentType(int id, string name, string templateName, Type type, TimeSpan? validityInterval = null) : base(id, name)
        {
            Type = type;
            TemplateName = templateName;
            ValidityInterval = validityInterval;
        }

        public static implicit operator DocumentType(int id) => Enumeration.FromValue<DocumentType>(id);
        public static implicit operator DocumentType(string name) => Enumeration.FromDisplayName<DocumentType>(name);

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
